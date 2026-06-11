using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Intento.Polling;

[PollingEventList]
public class IntentoLqaPollingList(InvocationContext invocationContext) : IntentoInvocable(invocationContext)
{
    private const string IntentoLqaStoragePath = "https://blackbird.io";

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
        var noFlightResponse = new PollingEventResponse<IntentoLqaReviewMemory, IntentoLqaPollingResponse>
        {
            FlyBird = false,
            Memory = new()
            {
                LastPollingTime = lastPollingTime,
                Triggered = false
            }
        };

        if (request.Memory is null)
            return noFlightResponse;

        foreach (var jobId in jobIds)
        {
            var statusRequest = new RestRequest($"/storage/action/status/{jobId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<StorageActionStatusResponseDto>(statusRequest);

            if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new PluginApplicationException(
                    $"Intento LQA job failed. Status response: {JsonConvert.SerializeObject(status)}");

            if (!string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase))
                return noFlightResponse;
        }

        var items = await GetSegmentsBySearchKeys(
            input.TargetLanguage,
            searchKeys);

        var evaluatedKeys = items
            .Where(x => !string.IsNullOrWhiteSpace(x.SearchKey) && x.Evaluation != null)
            .Select(x => x.SearchKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!searchKeys.All(evaluatedKeys.Contains))
            return noFlightResponse;

        return new()
        {
            FlyBird = !request.Memory.Triggered,
            Result = new()
            {
                JobIds = jobIds,
                SearchKeys = searchKeys
            },
            Memory = new()
            {
                LastPollingTime = lastPollingTime,
                Triggered = true
            }
        };
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
            request.AddHeader("x-storage-path", IntentoLqaStoragePath);
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
