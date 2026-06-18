using Apps.Intento.DataHandlers;
using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;

namespace Apps.Intento.Model.Request;

public class DownloadBackgroundIntentoReviewRequest
{
    [Display("Transformation file", Description = "File returned from 'Review with Intento LQA(background)'")]
    public FileReference File { get; set; } = default!;

    [Display("Search keys", Description = "Optional when the transformation file already contains stored Intento background metadata.")]
    public IEnumerable<string>? SearchKeys { get; set; }

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Score threshold")]
    [StaticDataSource(typeof(ThresholdDataHandler))]
    public double? ScoreThreshold { get; set; }

    [Display("Text verdict threshold", Description = "Use this instead of Score threshold to finalize segments by text verdict.")]
    [StaticDataSource(typeof(IntentoLqaTextThresholdDataHandler))]
    public string? TextScoreThreshold { get; set; }

    [Display("Add score as note", Description = "When enabled, adds the chosen Intento LQA score or verdict to segment notes.")]
    public bool? AddScoreToSegmentComment { get; set; } = true;

    [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
    [StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }
}
