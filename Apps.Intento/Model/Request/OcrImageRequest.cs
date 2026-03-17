using Apps.Intento.DataHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Intento.Model.Request;

public class OcrImageRequest
{
    [Display("Image")]
    public FileReference Image { get; set; } = default!;

    [Display("Provider")]
    [DataSource(typeof(OcrProviderDataHandler))]
    public string? Provider { get; set; }
}
