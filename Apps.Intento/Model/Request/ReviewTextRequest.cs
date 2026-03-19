using Apps.Intento.DataHandlers;
using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.Intento.Model.Request;

public class ReviewTextRequest : IReviewTextInput
{
    [Display("Source text")]
    public string SourceText { get; set; } = string.Empty;

    [Display("Target text")]
    public string TargetText { get; set; } = string.Empty;

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Model")]
    [StaticDataSource(typeof(ReviewModelDataHandler))]
    public string Model { get; set; } = "comet-mtqe";

    [Display("Score threshold")]
    public double? ScoreThreshold { get; set; }
   
}
