using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class OperationCreatedResponseDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}

