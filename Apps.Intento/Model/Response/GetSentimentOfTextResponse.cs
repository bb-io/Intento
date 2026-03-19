using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class GetSentimentOfTextResponse
{
    [Display("Sentiment label")]
    public string? SentimentLabel { get; set; }

    [Display("Sentiment score")]
    public double? SentimentScore { get; set; }

    [Display("Sentiment confidence")]
    public double? SentimentConfidence { get; set; }

    [Display("Sentiment subjectivity")]
    public string? SentimentSubjectivity { get; set; }

    [Display("Agreement")]
    public bool? Agreement { get; set; }

    [Display("Irony")]
    public bool? Irony { get; set; }

    [Display("Provider ID")]
    public string? ProviderId { get; set; }
}