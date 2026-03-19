using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class ClassifyTextResponse
{
    [Display("Classification JSON")]
    public string ClassificationJson { get; set; } = string.Empty;

    [Display("Provider ID")]
    public string? ProviderId { get; set; }
}