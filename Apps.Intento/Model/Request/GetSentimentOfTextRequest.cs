using Apps.Intento.DataHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.Model.Request;

public class GetSentimentOfTextRequest
{
    [Display("Text")]
    public string Text { get; set; } = string.Empty;

    [Display("Language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string Language { get; set; } = string.Empty;

    [Display("Provider")]
    public string? Provider { get; set; }
}