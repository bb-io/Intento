using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Model.Dto;

public class UsageStatisticsDto
{
    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("data")]
    public JToken? Data { get; set; }
}

public class UsageBucketDto
{
    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("metrics")]
    public UsageMetricsDto? Metrics { get; set; }
}

public class UsageMetricsDto
{
    [JsonProperty("requests")]
    public long Requests { get; set; }

    [JsonProperty("items")]
    public long Items { get; set; }

    [JsonProperty("len")]
    public long Length { get; set; }

    [JsonProperty("words")]
    public long Words { get; set; }

    [JsonProperty("errors")]
    public long Errors { get; set; }
}