using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Model.Dto;

public class StoreSegmentsResponseDto
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("searchKeys")]
    public List<string>? SearchKeys { get; set; }
}

public class RunStorageActionResponseDto
{
    [JsonProperty("jobId")]
    public string JobId { get; set; } = string.Empty;
}

public class StorageActionStatusResponseDto
{
    [JsonProperty("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonProperty("progress")]
    public double? Progress { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("results")]
    public StorageActionStatusResultsDto? Results { get; set; }
}

public class StorageActionStatusResultsDto
{
    [JsonProperty("completed")]
    public List<string>? Completed { get; set; }

    [JsonProperty("failed")]
    public List<string>? Failed { get; set; }
}

public class SearchSegmentsResponseDto
{
    [JsonProperty("items")]
    public List<SearchSegmentItemDto>? Items { get; set; }
}

public class SearchSegmentItemDto
{
    [JsonProperty("searchKey")]
    public string? SearchKey { get; set; }

    [JsonProperty("meta")]
    public SearchSegmentMetaDto? Meta { get; set; }

    [JsonProperty("evaluation")]
    public SearchSegmentEvaluationDto? Evaluation { get; set; }
}

public class SearchSegmentMetaDto
{
    [JsonProperty("externalKey")]
    public string? ExternalKey { get; set; }
}

public class SearchSegmentEvaluationDto
{
    [JsonProperty("score")]
    public double? Score { get; set; }

    [JsonProperty("scoreType")]
    public string? ScoreType { get; set; }

    [JsonProperty("details")]
    public SearchSegmentEvaluationDetailsDto? Details { get; set; }
}

public class SearchSegmentEvaluationDetailsDto
{
    [JsonProperty("finalScore")]
    public double? FinalScore { get; set; }

    [JsonProperty("scoreType")]
    public string? ScoreType { get; set; }

    [JsonProperty("ruleBasedResults")]
    public JObject? RuleBasedResults { get; set; }

    [JsonProperty("openaiResults")]
    public JObject? OpenAiResults { get; set; }
}
