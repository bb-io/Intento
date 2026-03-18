using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Apps.Intento.Service;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Intento.Actions;

[ActionList("Usage")]
public class UsageActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : IntentoInvocable(invocationContext)
{
    [Action("Get usage statistics", Description = "Get usage statistics from Intento")]
    public async Task<GetUsageStatisticsResponse> GetUsageStatistics([ActionParameter] GetUsageStatisticsRequest input)
    {
        var endpoint = input.EndpointType switch
        {
            "intento" => "/usage/intento",
            "provider" => "/usage/provider",
            _ => throw new PluginMisconfigurationException("Unsupported endpoint type")
        };

        var request = new RestRequest(endpoint, Method.Post);

        var body = RequestBuilder.BuildUsageStatisticsPayload(input);
        request.AddStringBody(body, ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<UsageStatisticsDto>(request);

        var buckets = new List<UsageBucketDto>();

        if (response.Data is JArray array)
        {
            buckets = array.ToObject<List<UsageBucketDto>>() ?? [];
        }
        else if (response.Data is JValue value && value.Type == JTokenType.String)
        {
        }
        else if (response.Data != null && response.Data.Type != JTokenType.Null)
        {
            throw new PluginApplicationException(
                $"Unsupported usage response format: {response.Data.Type}. Raw response: {response.Data}");
        }

        var totalRequests = buckets.Sum(x => x.Metrics?.Requests ?? 0);
        var totalItems = buckets.Sum(x => x.Metrics?.Items ?? 0);
        var totalLength = buckets.Sum(x => x.Metrics?.Length ?? 0);
        var totalWords = buckets.Sum(x => x.Metrics?.Words ?? 0);
        var totalErrors = buckets.Sum(x => x.Metrics?.Errors ?? 0);

        return new GetUsageStatisticsResponse
        {
            TotalRequests = totalRequests,
            TotalItems = totalItems,
            TotalLength = totalLength,
            TotalWords = totalWords,
            TotalErrors = totalErrors,
            BucketsJson = response.Data?.ToString(Formatting.Indented) ?? string.Empty
        };
    }
}

