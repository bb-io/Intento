using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Model.Dto;

public class GlossariesResponseDto
{
    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("glossaries")]
    public List<GlossaryDto> Glossaries { get; set; } = [];
}

public class GlossaryDto
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("has_draft")]
    public bool HasDraft { get; set; }

    [JsonProperty("entries_count")]
    public int EntriesCount { get; set; }

    [JsonProperty("language_pairs")]
    public List<GlossaryLanguagePairDto> LanguagePairs { get; set; } = [];

    [JsonProperty("terms")]
    public List<GlossaryTermDto> Terms { get; set; } = [];
}

public class GlossaryLanguagePairDto
{
    [JsonProperty("source")]
    public string Source { get; set; } = string.Empty;

    [JsonProperty("target")]
    public string Target { get; set; } = string.Empty;
}

public class GlossaryTermDto
{
    [JsonProperty("term")]
    public GlossaryTermValueDto? Term { get; set; }
}

public class GlossaryTermValueDto
{
    [JsonProperty("src")]
    public string Source { get; set; } = string.Empty;

    [JsonProperty("tgt")]
    public string? Target { get; set; }

    [JsonProperty("flgs")]
    public int Flags { get; set; }

    [JsonProperty("cstm")]
    public JToken? CustomVariants { get; set; }
}

public class GlossaryOperationResponseDto
{
    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("count")]
    public int? Count { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}
