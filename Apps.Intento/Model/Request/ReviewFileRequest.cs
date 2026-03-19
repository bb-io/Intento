using Apps.Intento.DataHandlers;
using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.Intento.Model.Request;

public class ReviewFileRequest : IReviewFileInput
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? TargetLanguage { get; set; }

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Model")]
    [StaticDataSource(typeof(ReviewModelDataHandler))]
    public string Model { get; set; } = "comet-mtqe";

    [Display("Score threshold")]
    public double? ScoreThreshold { get; set; }

    [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
    [StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }
}
