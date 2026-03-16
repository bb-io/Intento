using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class TranslateTextRequestDto
{
    [JsonProperty("context")]
    public TranslateTextContextDto Context { get; set; } = new();

    [JsonProperty("service", NullValueHandling = NullValueHandling.Ignore)]
    public TranslateTextServiceDto? Service { get; set; }
}

public class TranslateTextContextDto
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("to")]
    public string TargetLanguage { get; set; } = string.Empty;

    [JsonProperty("from")]
    public string? SourceLanguage { get; set; }

    [JsonProperty("category")]
    public string? Category { get; set; }
}

public class TranslateTextServiceDto
{
    [JsonProperty("routing")]
    public string? Routing { get; set; }

    [JsonProperty("translation_storage")]
    public bool? ApplyTranslationStorage { get; set; }

    [JsonProperty("translation_storage_update")]
    public bool? UpdateTranslationStorage { get; set; }

    [JsonProperty("no_trace")]
    public bool? DisableNoTrace { get; set; }
}