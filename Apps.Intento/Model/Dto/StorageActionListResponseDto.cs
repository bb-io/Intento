using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class StorageActionListResponseDto
{
    [JsonProperty("actions")]
    public List<StorageActionListItemDto>? Actions { get; set; }
}

public class StorageActionListItemDto
{
    [JsonProperty("actionId")]
    public string ActionId { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }
}
