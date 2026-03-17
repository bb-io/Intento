using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class LanguageDto
{
    [JsonProperty("direction")]
    public string? Direction { get; set; }

    [JsonProperty("intento_code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("iso_name")]
    public string Name { get; set; } = string.Empty;
}
