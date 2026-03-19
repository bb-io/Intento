using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class OperationStatusResponseDto
{
    [JsonProperty("done")]
    public bool Done { get; set; }

    [JsonProperty("response")]
    public List<TranslateTextResponseDto>? Response { get; set; }

    [JsonProperty("error")]
    public object? Error { get; set; }
}
