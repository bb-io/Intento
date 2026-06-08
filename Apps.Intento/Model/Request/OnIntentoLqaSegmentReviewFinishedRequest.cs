using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Request;

public class OnIntentoLqaSegmentReviewFinishedRequest
{
    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];

    [Display("Search keys")]
    public IEnumerable<string> SearchKeys { get; set; } = [];

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;
}
