using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class GetMeaningsOfTextResponse
{
    [Display("Meanings JSON")]
    public string MeaningsJson { get; set; } = string.Empty;

    [Display("Provider ID")]
    public string? ProviderId { get; set; }
}