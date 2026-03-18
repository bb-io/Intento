using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class GetUsageStatisticsResponse
{
    [Display("Total requests")]
    public long TotalRequests { get; set; }

    [Display("Total items")]
    public long TotalItems { get; set; }

    [Display("Total length")]
    public long TotalLength { get; set; }

    [Display("Total words")]
    public long TotalWords { get; set; }

    [Display("Total errors")]
    public long TotalErrors { get; set; }

    [Display("Buckets JSON")]
    public string BucketsJson { get; set; } = string.Empty;
}