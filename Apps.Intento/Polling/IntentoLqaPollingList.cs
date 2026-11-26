using Apps.Intento.Constants;
using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Intento.Polling;

[PollingEventList]
public class IntentoLqaPollingList(InvocationContext invocationContext) : IntentoInvocable(invocationContext)
{
    [PollingEvent("On background review with Intento LQA finished", "Triggered when all provided Intento LQA search keys have evaluations.")]
    public async Task<PollingEventResponse<IntentoLqaReviewMemory, IntentoLqaPollingResponse>> OnIntentoLqaSegmentReviewFinished(
        PollingEventRequest<IntentoLqaReviewMemory> request,
        [PollingEventParameter] OnIntentoLqaSegmentReviewFinishedRequest input)
    {
        var jobIds = input.JobIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var searchKeys = input.SearchKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (jobIds.Count == 0)
            throw new PluginMisconfigurationException("At least one job ID must be provided.");

        if (searchKeys.Count == 0)
            throw new PluginMisconfigurationException("At least one search key must be provided.");

        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
            throw new PluginMisconfigurationException("Target language is required.");

        if (string.IsNullOrWhiteSpace(input.SourceLanguage))
            throw new PluginMisconfigurationException("Source language is required.");

        var lastPollingTime = DateTime.UtcNow;
        var searchKeyBatches = searchKeys
            .Chunk(IntentoLqaConstants.ActionBatchSize)
            .Select(x => x.ToList())
            .ToList();

        if (jobIds.Count != searchKeyBatches.Count)
            throw new PluginMisconfigurationException(
                "The number of job IDs does not match the number of Intento LQA search key batches.");

        var activeJobIds = request.Memory?.ActiveJobIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        if (activeJobIds?.Count != jobIds.Count)
            activeJobIds = jobIds;

        var retriedBatchIndexes = request.Memory?.RetriedBatchIndexes?.ToHashSet() ?? [];
        var triggered = request.Memory?.Triggered ?? false;

        if (request.Memory is null)
            return CreateNoFlightResponse(lastPollingTime, activeJobIds, retriedBatchIndexes, triggered);

        for (var batchIndex = 0; batchIndex < activeJobIds.Count; batchIndex++)
        {
            var jobId = activeJobIds[batchIndex];
            var statusRequest = new RestRequest($"/storage/action/status/{jobId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<StorageActionStatusResponseDto>(statusRequest);

            if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new PluginApplicationException(
                    $"Intento LQA job failed. Status response: {JsonConvert.SerializeObject(status)}");

            if (!string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase))
                return CreateNoFlightResponse(lastPollingTime, activeJobIds, retriedBatchIndexes, triggered);

            if (HasZeroCompletedAndFailed(status))
            {
                if (retriedBatchIndexes.Contains(batchIndex))
                    throw new PluginApplicationException(
                        $"Intento LQA job returned no completed or failed results after retry. Status response: {JsonConvert.SerializeObject(status)}");

                activeJobIds[batchIndex] = await RunIntentoLqaAction(
                    input.TargetLanguage,
                    searchKeyBatches[batchIndex]);
                retriedBatchIndexes.Add(batchIndex);

                return CreateNoFlightResponse(lastPollingTime, activeJobIds, retriedBatchIndexes, triggered);
            }
        }

        var items = await GetSegmentsBySearchKeys(
            input.TargetLanguage,
            searchKeys);

        var evaluatedKeys = items
            .Where(x => !string.IsNullOrWhiteSpace(x.SearchKey) && x.Evaluation != null)
            .Select(x => x.SearchKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!searchKeys.All(evaluatedKeys.Contains))
            return CreateNoFlightResponse(lastPollingTime, activeJobIds, retriedBatchIndexes, triggered);

        return new()
        {
            FlyBird = !triggered,
            Result = new()
            {
                JobIds = activeJobIds,
                SearchKeys = searchKeys
            },
            Memory = new()
            {
                LastPollingTime = lastPollingTime,
                Triggered = true,
                ActiveJobIds = activeJobIds,
                RetriedBatchIndexes = retriedBatchIndexes.ToList()
            }
        };
    }

    private static PollingEventResponse<IntentoLqaReviewMemory, IntentoLqaPollingResponse> CreateNoFlightResponse(
        DateTime lastPollingTime,
        List<string> activeJobIds,
        HashSet<int> retriedBatchIndexes,
        bool triggered)
    {
        return new()
        {
            FlyBird = false,
            Memory = new()
            {
                LastPollingTime = lastPollingTime,
                Triggered = triggered,
                ActiveJobIds = activeJobIds,
                RetriedBatchIndexes = retriedBatchIndexes.ToList()
            }
        };
    }

    private async Task<string> RunIntentoLqaAction(string targetLanguage, List<string> searchKeys)
    {
        var request = new RestRequest("/storage/action/run", Method.Post);
        request.AddHeader("x-storage-path", IntentoLqaConstants.StoragePath);
        var payload = new JObject
        {
            ["to"] = targetLanguage,
            ["searchKeys"] = JArray.FromObject(searchKeys),
            ["actionId"] = IntentoLqaConstants.ActionId,
            ["variables"] = new JObject()
        };

        request.AddStringBody(payload.ToString(Formatting.None), ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<RunStorageActionResponseDto>(request);
        if (string.IsNullOrWhiteSpace(response.JobId))
            throw new PluginApplicationException("Intento did not return job id.");

        return response.JobId;
    }

    private static bool HasZeroCompletedAndFailed(StorageActionStatusResponseDto status)
    {
        var completedCount = status.Results?.Completed?.Count ?? 0;
        var failedCount = status.Results?.Failed?.Count ?? 0;
        return completedCount == 0 && failedCount == 0;
    }

    private async Task<List<SearchSegmentItemDto>> GetSegmentsBySearchKeys(
        string targetLanguage,
        IEnumerable<string> searchKeys)
    {
        var items = new List<SearchSegmentItemDto>();
        foreach (var batch in searchKeys
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Chunk(50))
        {
            var request = new RestRequest($"/storage/segment/{targetLanguage}", Method.Get);
            request.AddHeader("x-storage-path", IntentoLqaConstants.StoragePath);
            foreach (var searchKey in batch)
            {
                request.AddQueryParameter("searchKeys[]", searchKey);
            }

            var response = await Client.ExecuteWithErrorHandling<SearchSegmentsResponseDto>(request);
            if (response.Items != null)
                items.AddRange(response.Items);
        }

        return items;
    }
}
