using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class SmartRoutingResponseDto
{
    [JsonProperty("data")]
    public List<SmartRoutingDto> Data { get; set; } = [];
}

public class SmartRoutingDto
{
    [JsonProperty("rt_id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("is_public")]
    public bool IsPublic { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("is_allowed")]
    public bool IsAllowed { get; set; }
}