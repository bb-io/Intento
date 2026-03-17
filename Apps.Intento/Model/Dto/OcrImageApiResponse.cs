using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class OcrImageApiResponse
{
    [JsonProperty("results")]
    public string? Results { get; set; }

    [JsonProperty("service")]
    public OcrImageServiceResponseDto? Service { get; set; }
}

public class OcrImageServiceResponseDto
{
    [JsonProperty("provider")]
    public OcrImageProviderResponseDto? Provider { get; set; }
}

public class OcrImageProviderResponseDto
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}