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
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Intento.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagement)
    : IntentoInvocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.ReviewText)]
    [Action("Review text", Description = "Review translation quality for source and target text")]
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
    [Action("Review", Description = "Review translation quality for a file")]
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
}
