using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Apps.Intento.Service;
using Apps.Intento.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Intento.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagement)
    : IntentoInvocable(invocationContext)
{
    private const int IntentoLqaActionBatchSize = 25;
    private const int IntentoLqaJobPollAttempts = 24;
    private const int IntentoLqaEvaluationPollAttempts = 12;
    private static readonly TimeSpan IntentoLqaPollInterval = TimeSpan.FromSeconds(5);
    private const string IntentoLqaActionId = "674f27c0d4496a22fb664db8";

    [BlueprintActionDefinition(BlueprintAction.ReviewText)]
    [Action("Review text", Description = "Review translation quality for source and target text using IntentoQA")]
    public async Task<ReviewTextResponse> ReviewText([ActionParameter] ReviewTextRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.SourceText))
            throw new PluginMisconfigurationException("Source text is required.");

        if (string.IsNullOrWhiteSpace(input.TargetText))
            throw new PluginMisconfigurationException("Target text is required.");

        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is required.");

        if (string.IsNullOrWhiteSpace(input.Model))
            throw new PluginMisconfigurationException("Model is required.");

        var request = new RestRequest("/evaluate/score", Method.Post);

        var body = RequestBuilder.BuildReviewTextPayload(
            input.SourceText,
            input.TargetText,
            input.TargetLanguage,
            input.Model,
            itemize: false);

        request.AddStringBody(body, ContentType.Json);

        var operation = await Client.ExecuteWithErrorHandling<OperationCreatedResponseDto>(request);

        if (string.IsNullOrWhiteSpace(operation.Id))
            throw new PluginApplicationException("Intento did not return operation id.");

        var score = await PollReviewTextScore(operation.Id);

        return new ReviewTextResponse
        {
            Score = (float)score,
            IsAboveThreshold = input.ScoreThreshold.HasValue ? score >= input.ScoreThreshold.Value : null
        };
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Review translation quality for a file using IntentoLQA")]
    public async Task<QualityEstimationResponse> ReviewFile([ActionParameter] ReviewFileRequest input)
    {
        if (input.File == null)
            throw new PluginMisconfigurationException("File is required.");

        var threshold = input.ScoreThreshold ?? 0.8;
        if (threshold < 0 || threshold > 1)
            throw new PluginMisconfigurationException("Score threshold must be in range 0..1.");

        if (string.IsNullOrWhiteSpace(input.Model))
            throw new PluginMisconfigurationException("Model is required.");

        using var stream = await fileManagement.DownloadAsync(input.File);
        var content = await Transformation.Parse(stream, input.File.Name);

        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is not defined. Provide Source language.");

        if (string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is not defined. Provide Target language.");

        var sourceLanguage = content.SourceLanguage!;
        var targetLanguage = content.TargetLanguage!;

        int processedSegmentsCount = 0;
        int finalizedSegmentsCount = 0;
        int underThresholdCount = 0;
        double totalScore = 0.0;

        bool SegmentFilter(Segment s)
        {
            if (s == null) return false;
            if (s.IsIgnorbale) return false;
            if (s.State == SegmentState.Final) return false;

            var source = LineElementMapper.RenderLine(s.Source);
            var target = LineElementMapper.RenderLine(s.Target);

            if (string.IsNullOrWhiteSpace(source)) return false;
            if (string.IsNullOrWhiteSpace(target)) return false;

            return true;
        }

        var units = content.GetUnits().ToList();

        var processed = await units
            .Batch(batchSize: 25, segmentFilter: SegmentFilter)
            .Process<double>(async batch =>
            {
                var sourceTexts = batch
                    .Select(x => LineElementMapper.RenderLine(x.Segment.Source))
                    .ToList();

                var targetTexts = batch
                    .Select(x => LineElementMapper.RenderLine(x.Segment.Target))
                    .ToList();

                var scores = await ReviewBatchViaScoreEndpoint(
                    sourceTexts,
                    targetTexts,
                    targetLanguage,
                    input.Model);

                if (scores.Count != sourceTexts.Count)
                {
                    scores = scores
                        .Take(sourceTexts.Count)
                        .Concat(Enumerable.Repeat(0.0, Math.Max(0, sourceTexts.Count - scores.Count)))
                        .ToList();
                }

                return scores;
            });

        foreach ((Unit Unit, IEnumerable<(Segment Segment, double Result)> Results) item in processed)
        {
            double unitScoreSum = 0.0;
            int unitCount = 0;

            foreach ((Segment Segment, double Result) r in item.Results)
            {
                processedSegmentsCount++;
                totalScore += r.Result;

                unitScoreSum += r.Result;
                unitCount++;

                if (r.Result >= threshold)
                {
                    r.Segment.State = SegmentState.Final;
                    finalizedSegmentsCount++;
                }
                else
                {
                    underThresholdCount++;
                }
            }

            if (unitCount > 0)
            {
                item.Unit.Quality.ProfileReference = "https://api.inten.to/evaluate/score";
                item.Unit.Quality.ScoreThreshold = threshold;
                item.Unit.Quality.Score = (float)(unitScoreSum / unitCount);
            }
        }

        var finalFile = await BuildReviewedFile(content, input);

        var avgMetric = processedSegmentsCount > 0 ? (float)(totalScore / processedSegmentsCount) : 0f;
        var pctUnder = processedSegmentsCount > 0 ? (float)underThresholdCount / processedSegmentsCount : 0f;

        return new QualityEstimationResponse
        {
            File = finalFile,
            TotalSegmentsProcessed = processedSegmentsCount,
            TotalSegmentsFinalized = finalizedSegmentsCount,
            TotalSegmentsUnderThreshhold = underThresholdCount,
            AverageMetric = avgMetric,
            PercentageSegmentsUnderThreshhold = pctUnder
        };
    }

    [Action("Review with Intento LQA", Description = "Review translation quality for a file via Intento Translation Storage and Intento LQA action")]
    public async Task<QualityEstimationResponse> ReviewFileWithIntentoLqa([ActionParameter] ReviewFileWithIntentoLqaRequest input)
    {
        if (input.File == null)
            throw new PluginMisconfigurationException("File is required.");

        var threshold = input.ScoreThreshold ?? 0.8;
        if (threshold < 0 || threshold > 1)
            throw new PluginMisconfigurationException("Score threshold must be in range 0..1.");

        using var stream = await fileManagement.DownloadAsync(input.File);
        var content = await Transformation.Parse(stream, input.File.Name);

        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is not defined. Provide Source language.");

        if (string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is not defined. Provide Target language.");

        var sourceLanguage = content.SourceLanguage!;
        var targetLanguage = content.TargetLanguage!;

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(input.File.Name);
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        fileNameWithoutExtension = "file";

        var runId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        string operationPath = $"https://blackbird.io/{runId}/intento-lqa/{fileNameWithoutExtension}";
        var segmentRecords = BuildIntentoLqaSegmentRecords(content, sourceLanguage, targetLanguage, operationPath);
        if (!segmentRecords.Any())
            throw new PluginApplicationException("No reviewable segments were found in the file.");

        await StoreSegments(segmentRecords);

        var actionId = IntentoLqaActionId;
        var expectedSearchKeys = segmentRecords
            .Select(x => x.SearchKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var evaluations = new Dictionary<string, SearchSegmentEvaluationDto>(StringComparer.OrdinalIgnoreCase);
        var searchKeyBatches = expectedSearchKeys
            .Chunk(IntentoLqaActionBatchSize)
            .Select(x => x.ToList())
            .ToList();

        for (var batchIndex = 0; batchIndex < searchKeyBatches.Count; batchIndex++)
        {
            var batchSearchKeys = searchKeyBatches[batchIndex];
            var batchSearchKeySet = batchSearchKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var batchRecords = segmentRecords
                .Where(x => batchSearchKeySet.Contains(x.SearchKey))
                .ToList();

            var jobId = await RunIntentoLqaAction(actionId, targetLanguage, batchSearchKeys, operationPath);
            var jobStatus = await WaitForIntentoLqaJob(jobId);

            var batchEvaluations = await FetchEvaluationsWithSingleRetry(
                actionId,
                targetLanguage,
                sourceLanguage,
                operationPath,
                batchRecords,
                batchSearchKeys,
                jobStatus);

            foreach (var evaluation in batchEvaluations)
            {
                evaluations[evaluation.Key] = evaluation.Value;
            }
        }

        var processedSegmentsCount = 0;
        var finalizedSegmentsCount = 0;
        var underThresholdCount = 0;
        double totalScore = 0.0;
        var recordedNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in segmentRecords)
        {
            if (!evaluations.TryGetValue(record.SearchKey, out var evaluation) || evaluation.Score == null)
                continue;

            var normalizedScore = NormalizeIntentoScore(evaluation.Score.Value);
            processedSegmentsCount++;
            totalScore += normalizedScore;

            if (evaluation.Details?.OpenAiResults != null)
            {
                AddIntentoNotes(
                    record.Unit,
                    evaluation.Details.OpenAiResults["content"]?["errors"] as JArray,
                    source: "intento-openai",
                    recordedNotes);
            }

            if (evaluation.Details?.RuleBasedResults != null)
            {
                AddIntentoNotes(
                    record.Unit,
                    evaluation.Details.RuleBasedResults["content"] as JArray,
                    source: "intento-rule-based",
                    recordedNotes);
            }

            record.Unit.Quality.ProfileReference = "Intento LQA";
            record.Unit.Quality.ScoreThreshold = threshold;
            record.Unit.Quality.Score = (float)normalizedScore;

            if (normalizedScore >= threshold)
            {
                record.Segment.State = SegmentState.Final;
                finalizedSegmentsCount++;
            }
            else
            {
                underThresholdCount++;
            }
        }

        var outputFileHandling = string.Equals(input.OutputFileHandling, "xliff1", StringComparison.OrdinalIgnoreCase)
            ? "xliff1"
            : null;

        var finalFile = await BuildReviewedFile(content, new ReviewFileRequest
        {
            File = input.File,
            OutputFileHandling = outputFileHandling
        });

        var avgMetric = processedSegmentsCount > 0 ? (float)(totalScore / processedSegmentsCount) : 0f;
        var pctUnder = processedSegmentsCount > 0 ? (float)underThresholdCount / processedSegmentsCount : 0f;

        return new QualityEstimationResponse
        {
            File = finalFile,
            TotalSegmentsProcessed = processedSegmentsCount,
            TotalSegmentsFinalized = finalizedSegmentsCount,
            TotalSegmentsUnderThreshhold = underThresholdCount,
            AverageMetric = avgMetric,
            PercentageSegmentsUnderThreshhold = pctUnder
        };
    }

    private async Task<List<double>> ReviewBatchViaScoreEndpoint(
        List<string> sourceTexts,
        List<string> targetTexts,
        string targetLanguage,
        string model)
    {
        var request = new RestRequest("/evaluate/score", Method.Post);

        var body = RequestBuilder.BuildReviewBatchPayload(
            sourceTexts,
            targetTexts,
            targetLanguage,
            model,
            itemize: true);

        request.AddStringBody(body, ContentType.Json);

        var operation = await Client.ExecuteWithErrorHandling<OperationCreatedResponseDto>(request);

        if (string.IsNullOrWhiteSpace(operation.Id))
            throw new PluginApplicationException("Intento did not return operation id.");

        return await PollReviewBatchScores(operation.Id);
    }

    private async Task<double> PollReviewTextScore(string operationId)
    {
        for (var i = 0; i < 60; i++)
        {
            var request = new RestRequest($"/evaluate/score/{operationId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<ScoreOperationStatusResponseDto>(request);

            if (status.Done)
            {
                if (status.Error != null)
                    throw new PluginApplicationException($"Intento review operation failed: {status.Error}");

                var firstScore = status.Response?.Results?.Scores?.FirstOrDefault();
                if (firstScore?.Value == null)
                    throw new PluginApplicationException("Intento review operation completed but returned no score.");

                return ExtractReviewScore(firstScore.Value);
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException("Intento review polling timed out.");
    }

    private async Task<List<double>> PollReviewBatchScores(string operationId)
    {
        for (var i = 0; i < 60; i++)
        {
            var request = new RestRequest($"/evaluate/score/{operationId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<ScoreOperationStatusResponseDto>(request);

            if (status.Done)
            {
                if (status.Error != null)
                    throw new PluginApplicationException($"Intento review operation failed: {status.Error}");

                var firstScore = status.Response?.Results?.Scores?.FirstOrDefault();
                if (firstScore?.Value == null)
                    throw new PluginApplicationException("Intento review operation completed but returned no scores.");

                return ExtractSegmentScores(firstScore.Value);
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException("Intento review polling timed out.");
    }

    private static double ExtractReviewScore(object value)
    {
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is long l) return l;
        if (value is int i) return i;

        var token = Newtonsoft.Json.Linq.JToken.FromObject(value);

        if (token.Type == Newtonsoft.Json.Linq.JTokenType.Float ||
            token.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
        {
            return token.Value<double>();
        }

        var segmentScore = token["segment_scores"]?.FirstOrDefault();
        if (segmentScore != null)
            return segmentScore.Value<double>();

        var corpusScore = token["corpus_scores"]?.FirstOrDefault();
        if (corpusScore != null)
            return corpusScore.Value<double>();

        throw new PluginApplicationException("Unsupported score response format.");
    }

    private static List<double> ExtractSegmentScores(object value)
    {
        var token = Newtonsoft.Json.Linq.JToken.FromObject(value);

        var segmentScores = token["segment_scores"];
        if (segmentScores == null)
            throw new PluginApplicationException("Intento review operation returned no segment scores.");

        return segmentScores
            .Select(x => x.Value<double>())
            .ToList();
    }

    private List<IntentoLqaSegmentRecord> BuildIntentoLqaSegmentRecords(
        Transformation content,
        string sourceLanguage,
        string targetLanguage,
        string operationPath)
    {
        var records = new List<IntentoLqaSegmentRecord>();
        var seenExternalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var units = content.GetUnits().ToList();

        for (var unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            var unit = units[unitIndex];
            for (var segmentIndex = 0; segmentIndex < unit.Segments.Count; segmentIndex++)
            {
                var segment = unit.Segments[segmentIndex];
                if (!ShouldReviewSegment(segment))
                    continue;

                var source = LineElementMapper.RenderLine(segment.Source);
                var target = LineElementMapper.RenderLine(segment.Target);
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                    continue;

                var externalKey = BuildExternalKey(unit, unitIndex, segmentIndex);
                if (!seenExternalKeys.Add(externalKey))
                    continue;

                records.Add(new IntentoLqaSegmentRecord
                {
                    Unit = unit,
                    Segment = segment,
                    SourceText = source,
                    TargetText = target,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    ExternalKey = externalKey,
                    SearchKey = CreateSearchKey(source),
                    Path = operationPath
                });
            }
        }

        return records;
    }

    private async Task StoreSegments(List<IntentoLqaSegmentRecord> records)
    {
        foreach (var batch in records.Chunk(25))
        {
            var batchRecords = batch.ToList();
            var payload = new JObject
            {
                ["segments"] = new JArray(batchRecords.Select(record => new JObject
                {
                    ["to"] = record.TargetLanguage,
                    ["from"] = record.SourceLanguage,
                    ["source"] = record.SourceText,
                    ["target"] = record.TargetText,
                    ["type"] = "ht",
                    ["origin"] = "Blackbird",
                    ["meta"] = new JObject
                    {
                        ["externalKey"] = record.ExternalKey
                    },
                    ["path"] = record.Path
                }))
            };

            var request = new RestRequest("/storage/segment", Method.Post);
            request.AddStringBody(payload.ToString(Formatting.None), ContentType.Json);

            var response = await Client.ExecuteWithErrorHandling<StoreSegmentsResponseDto>(request);
            if (!response.Success || response.SearchKeys == null || response.SearchKeys.Count != batchRecords.Count)
            {
                throw new PluginApplicationException("Intento did not return search keys for all stored segments.");
            }

            for (var i = 0; i < batchRecords.Count; i++)
            {
                batchRecords[i].SearchKey = response.SearchKeys[i];
            }
        }
    }

    private async Task<string> RunIntentoLqaAction(
        string actionId,
        string targetLanguage,
        List<string> searchKeys,
        string operationPath)
    {
        var request = new RestRequest("/storage/action/run", Method.Post);
        request.AddHeader("x-storage-path", operationPath);
        var payload = new JObject
        {
            ["to"] = targetLanguage,
            ["searchKeys"] = JArray.FromObject(searchKeys),
            ["actionId"] = actionId,
            ["variables"] = new JObject()
        };

        request.AddStringBody(payload.ToString(Formatting.None), ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<RunStorageActionResponseDto>(request);
        if (string.IsNullOrWhiteSpace(response.JobId))
            throw new PluginApplicationException("Intento did not return job id.");

        return response.JobId;
    }

    private async Task WaitForStoredSegmentsReady(
        string targetLanguage,
        string sourceLanguage,
        List<IntentoLqaSegmentRecord> records,
        string operationPath)
    {
        var expectedKeys = records
            .Select(x => x.ExternalKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, SearchSegmentItemDto> storedSegments = new(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 60; attempt++)
        {
            storedSegments = new Dictionary<string, SearchSegmentItemDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in records)
            {
                var item = await SearchSegmentByExternalKey(
                    targetLanguage,
                    sourceLanguage,
                    operationPath,
                    record.ExternalKey);

                if (item != null)
                {
                    storedSegments[record.ExternalKey] = item;
                }
            }

            if (storedSegments.Keys.Count(expectedKeys.Contains) == expectedKeys.Count)
                return;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException(
            $"Intento LQA stored segments were not visible in storage search. Expected {expectedKeys.Count} keys, found {storedSegments.Count} segments.");
    }

    private async Task<StorageActionStatusResponseDto> WaitForIntentoLqaJob(string jobId)
    {
        for (var i = 0; i < IntentoLqaJobPollAttempts; i++)
        {
            var request = new RestRequest($"/storage/action/status/{jobId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<StorageActionStatusResponseDto>(request);

            if (string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }

            if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new PluginApplicationException(
                    $"Intento LQA job failed. Status response: {JsonConvert.SerializeObject(status)}");

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException("Intento LQA job polling timed out.");
    }

    private async Task<Dictionary<string, SearchSegmentEvaluationDto>> FetchEvaluationsWithSingleRetry(
        string actionId,
        string targetLanguage,
        string sourceLanguage,
        string operationPath,
        List<IntentoLqaSegmentRecord> records,
        List<string> expectedSearchKeys,
        StorageActionStatusResponseDto jobStatus)
    {
        try
        {
            return await FetchIntentoLqaEvaluations(targetLanguage, sourceLanguage, records, operationPath);
        }
        catch (PluginApplicationException ex) when (ShouldRetryEmptyEvaluationMaterialization(jobStatus, ex))
        {
            var retryJobId = await RunIntentoLqaAction(actionId, targetLanguage, expectedSearchKeys, operationPath);
            var retryStatus = await WaitForIntentoLqaJob(retryJobId);

            return await FetchIntentoLqaEvaluations(targetLanguage, sourceLanguage, records, operationPath);
        }
    }

    private async Task<Dictionary<string, SearchSegmentEvaluationDto>> FetchIntentoLqaEvaluations(
        string targetLanguage,
        string sourceLanguage,
        List<IntentoLqaSegmentRecord> records,
        string operationPath)
    {
        var expectedKeys = records
            .Select(x => x.SearchKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, SearchSegmentEvaluationDto> evaluations = new(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < IntentoLqaEvaluationPollAttempts; attempt++)
        {
            evaluations = new Dictionary<string, SearchSegmentEvaluationDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in records)
            {
                var item = await SearchSegmentByExternalKeyAndSearchKey(
                    targetLanguage,
                    sourceLanguage,
                    operationPath,
                    record.ExternalKey,
                    record.SearchKey);

                if (item?.Evaluation != null)
                {
                    evaluations[record.SearchKey] = item.Evaluation;
                }
            }

            if (evaluations.Keys.Count(expectedKeys.Contains) == expectedKeys.Count)
                return evaluations;

            await Task.Delay(IntentoLqaPollInterval);
        }

        throw new PluginApplicationException(
            $"Intento LQA evaluations were not materialized in storage after job success. Expected {expectedKeys.Count} keys, found {evaluations.Count} evaluations.");
    }

    private static bool ShouldRetryEmptyEvaluationMaterialization(
        StorageActionStatusResponseDto status,
        PluginApplicationException ex)
    {
        return HasZeroCompletedAndFailed(status)
            && ex.Message.Contains(
                "Intento LQA evaluations were not materialized in storage after job success",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasZeroCompletedAndFailed(StorageActionStatusResponseDto status)
    {
        var completedCount = status.Results?.Completed?.Count ?? 0;
        var failedCount = status.Results?.Failed?.Count ?? 0;
        return string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase)
            && completedCount == 0
            && failedCount == 0;
    }

    private async Task<SearchSegmentItemDto?> SearchSegmentByExternalKeyAndSearchKey(
        string targetLanguage,
        string sourceLanguage,
        string operationPath,
        string externalKey,
        string searchKey)
    {
        var request = new RestRequest($"/storage/segment/{targetLanguage}/search", Method.Get);
        request.AddHeader("x-storage-path", operationPath);
        request.AddQueryParameter("limit", "50");
        request.AddQueryParameter("from", sourceLanguage);
        request.AddQueryParameter("type", "ht");
        request.AddQueryParameter("textSearchMode", "external-key");
        request.AddQueryParameter("keywords", externalKey);

        var response = await Client.ExecuteWithErrorHandling<SearchSegmentsResponseDto>(request);
        return response.Items?
            .FirstOrDefault(x => string.Equals(x.SearchKey, searchKey, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SearchSegmentItemDto?> SearchSegmentByExternalKey(
        string targetLanguage,
        string sourceLanguage,
        string operationPath,
        string externalKey)
    {
        var request = new RestRequest($"/storage/segment/{targetLanguage}/search", Method.Get);
        request.AddHeader("x-storage-path", operationPath);
        request.AddQueryParameter("limit", "50");
        request.AddQueryParameter("from", sourceLanguage);
        request.AddQueryParameter("type", "ht");
        request.AddQueryParameter("textSearchMode", "external-key");
        request.AddQueryParameter("keywords", externalKey);

        var response = await Client.ExecuteWithErrorHandling<SearchSegmentsResponseDto>(request);
        return response.Items?
            .FirstOrDefault(x => string.Equals(x.Meta?.ExternalKey, externalKey, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddIntentoNotes(Unit unit, JArray? errors, string source, HashSet<string> recordedNotes)
    {
        if (errors == null || errors.Count == 0)
            return;

        unit.Notes ??= [];

        foreach (var errorToken in errors.OfType<JObject>())
        {
            var type = errorToken["error_type"]?.ToString()
                ?? errorToken["errorType"]?.ToString();
            var description = errorToken["description"]?.ToString();
            var severity = errorToken["severity"]?.ToString();

            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(description))
                continue;

            var normalizedType = NormalizeNoteCategoryPart(type);
            var category = $"{source}:{normalizedType}";
            var text = string.IsNullOrWhiteSpace(severity)
                ? description.Trim()
                : $"[{severity.Trim()}] {description.Trim()}";
            var noteKey = $"{unit.Id}|{category}|{text}";

            if (!recordedNotes.Add(noteKey))
                continue;

            unit.Notes.Add(new Note(text)
            {
                Category = category
            });
        }
    }

    private static string NormalizeNoteCategoryPart(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        return new string(normalized
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray());
    }

    private static bool ShouldReviewSegment(Segment segment)
    {
        if (segment == null || segment.IsIgnorbale)
            return false;

        var source = LineElementMapper.RenderLine(segment.Source);
        var target = LineElementMapper.RenderLine(segment.Target);

        return !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target);
    }

    private static string BuildExternalKey(Unit unit, int unitIndex, int segmentIndex)
    {
        var baseKey = !string.IsNullOrWhiteSpace(unit.Id)
            ? unit.Id
            : !string.IsNullOrWhiteSpace(unit.Name)
                ? unit.Name
                : $"unit-{unitIndex + 1}";

        return $"{baseKey}|seg-{segmentIndex + 1}";
    }

    private static string CreateSearchKey(string source)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(source);
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static double NormalizeIntentoScore(double score) => score > 1 ? score / 100d : score;

    private async Task<Blackbird.Applications.Sdk.Common.Files.FileReference> BuildReviewedFile(
        Transformation content,
        ReviewFileRequest input)
    {
        if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var targetContent = content.Target();
                return await fileManagement.UploadAsync(
                    targetContent.Serialize().ToStream(),
                    targetContent.OriginalMediaType ?? "application/octet-stream",
                    targetContent.OriginalName ?? input.File.Name);
            }
            catch
            {
                return await fileManagement.UploadAsync(
                    content.Serialize().ToStream(),
                    MediaTypes.Xliff,
                    content.XliffFileName);
            }
        }

        if (input.OutputFileHandling?.Equals("xliff1", StringComparison.OrdinalIgnoreCase) == true)
        {
            var xliff1String = Xliff1Serializer.Serialize(content);
            return await fileManagement.UploadAsync(
                xliff1String.ToStream(),
                MediaTypes.Xliff,
                content.XliffFileName);
        }

        return await fileManagement.UploadAsync(
            content.Serialize().ToStream(),
            MediaTypes.Xliff,
            content.XliffFileName);
    }

    private sealed class IntentoLqaSegmentRecord
    {
        public required Unit Unit { get; init; }

        public required Segment Segment { get; init; }

        public required string SourceText { get; init; }

        public required string TargetText { get; init; }

        public required string SourceLanguage { get; init; }

        public required string TargetLanguage { get; init; }

        public required string ExternalKey { get; init; }

        public required string SearchKey { get; set; }

        public required string Path { get; init; }
    }
}
