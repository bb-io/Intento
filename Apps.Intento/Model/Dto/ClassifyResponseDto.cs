using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Model.Dto;

public class ClassifyResponseDto
{
    [JsonProperty("results")]
    public JToken? Results { get; set; }

    [JsonProperty("service")]
    public OcrImageServiceResponseDto? Service { get; set; }
}
