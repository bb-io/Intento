using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Intento.Model.Request;

public class GetUsageStatisticsRequest
{
    [Display("Endpoint type")]
    [StaticDataSource(typeof(UsageEndpointTypeDataHandler))]
    public string EndpointType { get; set; } = "/usage/intento";

    [Display("Bucket")]
    [StaticDataSource(typeof(UsageBucketDataHandler))]
    public string? Bucket { get; set; }

    [Display("Items")]
    public int? Items { get; set; }

    [Display("From timestamp")]
    public long? From { get; set; }

    [Display("To timestamp")]
    public long? To { get; set; }

    [Display("Provider")]
    public string? Provider { get; set; }

    [Display("Intent")]
    public string? Intent { get; set; }

    [Display("Client")]
    public string? Client { get; set; }

    [Display("Status")]
    public string? Status { get; set; }

    [Display("Language pair")]
    public string? LanguagePair { get; set; }
}
