using Apps.Intento.DataHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Intento.Model.Request;

public class ReviewFileWithIntentoLqaBackgroundRequest
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }

    [Display("LQA action", Description = "Select the Intento LQA workflow configured for the required language pair under the connected API key.")]
    [DataSource(typeof(StorageActionDataHandler))]
    public string ActionId { get; set; } = string.Empty;

    [Display("State qualifiers", Description = "Only review segments whose target state-qualifier matches one of the provided values. Leave empty to review all eligible segments.")]
    public IEnumerable<string>? StateQualifiers { get; set; }
}
