using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Response;

public class IntentoLqaPollingResponse
{
    [Display("Job IDs")]
    public IEnumerable<string> JobIds { get; set; } = [];

    [Display("Search keys")]
    public IEnumerable<string> SearchKeys { get; set; } = [];
}
