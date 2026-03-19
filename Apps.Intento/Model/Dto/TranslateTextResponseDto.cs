using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class TranslateTextResponseDto
{
    [JsonProperty("results")]
    public List<string>? Results { get; set; }

    [JsonProperty("meta")]
    public TranslateTextMetaDto? Meta { get; set; }

    [JsonIgnore]
    public string TranslatedText => Results?.FirstOrDefault() ?? string.Empty;

    [JsonIgnore]
    public string? DetectedSourceLanguage => Meta?.DetectedSourceLanguage?.FirstOrDefault();
}

public class TranslateTextMetaDto
{
    [JsonProperty("detected_source_language")]
    public List<string>? DetectedSourceLanguage { get; set; }
}
