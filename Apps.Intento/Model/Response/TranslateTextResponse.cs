using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class TranslateTextResponse
{
    [Display("Translated text")]
    public string TranslatedText { get; set; } = string.Empty;

    [Display("Detected source language")]
    public string? DetectedSourceLanguage { get; set; }
}

