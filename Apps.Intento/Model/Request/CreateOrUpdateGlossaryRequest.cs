using Apps.Intento.DataHandlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Intento.Model.Request;

public class CreateOrUpdateGlossaryRequest
{
    [Display("Glossary", Description = "Blackbird interoperable TBX glossary")]
    public FileReference Glossary { get; set; } = default!;

    [Display("Glossary ID", Description = "Provide an existing glossary ID to update it; leave empty to create a new glossary")]
    public string? GlossaryId { get; set; }

    [Display("Glossary name", Description = "Required when the TBX file does not contain a title; ignored when updating")]
    public string? Name { get; set; }

    [Display("Glossary type")]
    [StaticDataSource(typeof(GlossaryTypeDataHandler))]
    public string? Type { get; set; }

    [Display("Source language", Description = "Language code of source terms in the TBX file")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language", Description = "Language code of target terms in the TBX file")]
    public string TargetLanguage { get; set; } = string.Empty;
}
