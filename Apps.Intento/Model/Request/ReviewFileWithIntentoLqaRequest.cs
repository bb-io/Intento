using Apps.Intento.DataHandlers;
using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;

namespace Apps.Intento.Model.Request;

public class ReviewFileWithIntentoLqaRequest
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

    [Display("Score threshold")]
    [StaticDataSource(typeof(ThresholdDataHandler))]
    public double? ScoreThreshold { get; set; }

    [Display("Text verdict threshold", Description = "Use this instead of Score threshold to finalize segments by text verdict.")]
    [StaticDataSource(typeof(IntentoLqaTextThresholdDataHandler))]
    public string? TextScoreThreshold { get; set; }

    [Display("Add score as note", Description = "When enabled, adds the chosen Intento LQA score or verdict to segment notes.")]
    public bool? AddScoreToSegmentComment { get; set; } = true;

    [Display("Add error comments as notes", Description = "When enabled, adds Intento LQA error details to segment notes.")]
    public bool? AddErrorComments { get; set; } = true;

    [Display("State qualifiers", Description = "Only review segments whose target state-qualifier matches one of the provided values. Leave empty to review all eligible segments.")]
    public IEnumerable<string>? StateQualifiers { get; set; }

    [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
    [StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }
}
