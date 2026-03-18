using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Model.Dto;

public class DictionaryApiResponse
{
    [JsonProperty("results")]
    public List<JObject>? Results { get; set; }

    [JsonProperty("service")]
    public OcrImageServiceResponseDto? Service { get; set; }
}