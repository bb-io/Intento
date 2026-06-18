using Apps.Intento.DataHandlers;
using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
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

    [Display("Score threshold")]
    [StaticDataSource(typeof(ThresholdDataHandler))]
    public double? ScoreThreshold { get; set; }

    [Display("Text verdict threshold", Description = "Use this instead of Score threshold to finalize segments by text verdict.")]
    [StaticDataSource(typeof(IntentoLqaTextThresholdDataHandler))]
    public string? TextScoreThreshold { get; set; }

    [Display("Add score as note", Description = "Stores whether the chosen Intento LQA score or verdict should be added to segment notes when results are downloaded.")]
    public bool? AddScoreToSegmentComment { get; set; } = true;
}
