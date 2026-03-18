using Apps.Intento.DataHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.Model.Request;

public class GetMeaningsOfTextRequest
{
    [Display("Text")]
    public string Text { get; set; } = string.Empty;

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Provider")]
    public string? Provider { get; set; }
}