using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class OcrProviderDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("own_auth")]
    public bool OwnAuth { get; set; }
}