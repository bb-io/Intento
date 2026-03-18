using Apps.Intento.Model.Dto;
using Newtonsoft.Json;

namespace Apps.Intento.Model.Request;

public class SentimentApiResponse
{
    [JsonProperty("results")]
    public List<SentimentResultDto>? Results { get; set; }

    [JsonProperty("service")]
    public OcrImageServiceResponseDto? Service { get; set; }
}

public class SentimentResultDto
{
    [JsonProperty("sentiment_label")]
    public string? SentimentLabel { get; set; }

    [JsonProperty("sentiment_score")]
    public double? SentimentScore { get; set; }

    [JsonProperty("sentiment_confidence")]
    public double? SentimentConfidence { get; set; }

    [JsonProperty("sentiment_subjectivity")]
    public string? SentimentSubjectivity { get; set; }

    [JsonProperty("agreement")]
    public bool? Agreement { get; set; }

    [JsonProperty("irony")]
    public bool? Irony { get; set; }
}