using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Intento.Model.Response;

public class ReviewFileWithIntentoLqaBackgroundResponse
{
    [Display("Transformation file")]
    public FileReference File { get; set; } = default!;

    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];

    [Display("Search keys")]
    public IEnumerable<string> SearchKeys { get; set; } = [];

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Total segments sent for review")]
    public int TotalSegmentsSentForReview { get; set; }
}
