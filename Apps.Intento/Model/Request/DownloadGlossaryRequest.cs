using Blackbird.Applications.Sdk.Common;

namespace Apps.Intento.Model.Request;

public class DownloadGlossaryRequest
{
    [Display("Glossary ID")]
    public string GlossaryId { get; set; } = string.Empty;

    [Display("Include draft terms", Description = "If true, download draft terms instead of only the currently published terms")]
    public bool? IncludeDraft { get; set; }
}
