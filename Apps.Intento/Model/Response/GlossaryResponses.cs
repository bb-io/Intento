using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Intento.Model.Response;

public class SearchGlossariesResponse
{
    public IEnumerable<GlossaryItemResponse> Glossaries { get; set; } = [];
}

public class GlossaryItemResponse
{
    [Display("Glossary ID")]
    public string GlossaryId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [Display("Has draft changes")]
    public bool HasDraft { get; set; }

    [Display("Entry count")]
    public int EntryCount { get; set; }

    [Display("Language pairs")]
    public IEnumerable<string> LanguagePairs { get; set; } = [];
}

public class CreateOrUpdateGlossaryResponse
{
    [Display("Glossary ID")]
    public string GlossaryId { get; set; } = string.Empty;

    [Display("Imported terms")]
    public int ImportedTerms { get; set; }
}

public class DownloadGlossaryResponse
{
    public FileReference Glossary { get; set; } = default!;

    [Display("Number of terms")]
    public int NumberOfTerms { get; set; }
}
