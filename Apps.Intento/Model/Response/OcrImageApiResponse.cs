using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class OcrImageResponse
{
    [Display("Text")]
    public string Text { get; set; } = string.Empty;

    [Display("Provider ID")]
    public string? ProviderId { get; set; }
}
