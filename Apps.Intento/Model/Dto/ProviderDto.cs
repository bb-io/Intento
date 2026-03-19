using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

internal class ProviderDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("vendor")]
    public string? Vendor { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("own_auth")]
    public bool OwnAuth { get; set; }

    [JsonProperty("stock_model")]
    public bool? StockModel { get; set; }

    [JsonProperty("custom_model")]
    public bool? CustomModel { get; set; }
}
