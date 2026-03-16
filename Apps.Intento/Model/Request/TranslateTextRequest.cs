using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.Intento.Model.Request;

public class TranslateTextRequest : ITranslateTextInput
{
    [Display("Text")]
    public string Text { get; set; } = string.Empty;

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Source language")]
    public string? SourceLanguage { get; set; }

    [Display("Smart routing")]
    public string? SmartRouting { get; set; }

    [Display("Apply translation storage")]
    public bool? ApplyTranslationStorage { get; set; }

    [Display("Update translation storage")]
    public bool? UpdateTranslationStorage { get; set; }

    [Display("Disable no trace")]
    public bool? DisableNoTrace { get; set; }
}