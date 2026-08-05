using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using System.Net.Mime;

namespace Apps.Intento.Actions;

[ActionList("Glossaries")]
public class GlossaryActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : IntentoInvocable(invocationContext)
{
    private const int DntGlossaryType = 2;
    private const int UnidirectionalGlossaryType = 1;

    [Action("Search glossaries", Description = "Search all available Intento glossaries")]
    public async Task<SearchGlossariesResponse> SearchGlossaries()
    {
        var request = new RestRequest("/ai/text/glossaries/v2/typed", Method.Get);
        var response = await Client.ExecuteWithErrorHandling<GlossariesResponseDto>(request);

        return new SearchGlossariesResponse
        {
            Glossaries = response.Glossaries.Select(glossary => new GlossaryItemResponse
            {
                GlossaryId = glossary.Id.ToString(),
                Name = glossary.Name,
                Type = FormatGlossaryType(glossary.Type),
                HasDraft = glossary.HasDraft,
                EntryCount = glossary.EntriesCount,
                LanguagePairs = glossary.LanguagePairs.Select(x => $"{x.Source}-{x.Target}")
            })
        };
    }

    [Action("Create or update glossary", Description = "Create a glossary or merge terms into an existing glossary from a Blackbird interoperable TBX file")]
    public async Task<CreateOrUpdateGlossaryResponse> CreateOrUpdateGlossary(
        [ActionParameter] CreateOrUpdateGlossaryRequest input)
    {
        if (input.Glossary == null)
            throw new PluginMisconfigurationException("Glossary file is required.");

        var existingGlossary = await GetExistingGlossary(input.GlossaryId);
        var glossaryType = ResolveGlossaryType(input.Type, existingGlossary?.Type);
        var sourceLanguage = NormalizeRequiredLanguage(input.SourceLanguage, "Source language");
        var targetLanguage = NormalizeRequiredLanguage(input.TargetLanguage, "Target language");

        if (existingGlossary != null)
            ValidateExistingGlossary(existingGlossary, glossaryType, sourceLanguage, targetLanguage);

        await using var glossaryStream = await fileManagementClient.DownloadAsync(input.Glossary);
        var interoperableGlossary = await glossaryStream.ConvertFromTbx();
        var terms = ExtractTerms(
            interoperableGlossary,
            sourceLanguage,
            targetLanguage,
            glossaryType);

        if (terms.Count == 0)
        {
            throw new PluginMisconfigurationException(
                "The glossary does not contain terms for the selected source and target languages.");
        }

        var glossaryId = existingGlossary?.Id ?? await CreateGlossary(
            input.Name ?? interoperableGlossary.Title,
            glossaryType,
            sourceLanguage,
            targetLanguage);

        var importedTerms = await ImportTerms(glossaryId, terms);

        return new CreateOrUpdateGlossaryResponse
        {
            GlossaryId = glossaryId.ToString(),
            ImportedTerms = importedTerms
        };
    }

    [Action("Download glossary", Description = "Download an Intento glossary as a Blackbird interoperable TBX file")]
    public async Task<DownloadGlossaryResponse> DownloadGlossary(
        [ActionParameter] DownloadGlossaryRequest input)
    {
        var glossaryId = ParseGlossaryId(input.GlossaryId);
        var request = new RestRequest($"/ai/text/glossaries/v2/typed/{glossaryId}", Method.Get)
            .AddQueryParameter("draft", input.IncludeDraft ?? false)
            .AddQueryParameter("terms", true);

        var glossary = await Client.ExecuteWithErrorHandling<GlossaryDto>(request);
        var languagePairs = glossary.LanguagePairs
            .Select(x => new
            {
                Source = NormalizeLanguageCode(x.Source),
                Target = NormalizeLanguageCode(x.Target)
            })
            .DistinctBy(x => $"{x.Source}\u001f{x.Target}", StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (languagePairs.Count > 1)
        {
            throw new PluginApplicationException(
                "The Intento glossary contains multiple language pairs, but its terms do not identify which target language they belong to. It cannot be downloaded as an interoperable bilingual glossary without selecting and storing each pair separately.");
        }

        var languagePair = languagePairs.FirstOrDefault();
        var sourceLanguage = NormalizeOutputLanguage(languagePair?.Source);
        var targetLanguage = NormalizeOutputLanguage(languagePair?.Target);

        var conceptEntries = glossary.Terms
            .Where(x => !string.IsNullOrWhiteSpace(x.Term?.Source))
            .Select((x, index) => CreateConceptEntry(
                index,
                x.Term!,
                sourceLanguage,
                targetLanguage,
                glossary.Type))
            .ToList();

        var interoperableGlossary = new Glossary(conceptEntries)
        {
            Title = string.IsNullOrWhiteSpace(glossary.Name)
                ? $"Intento glossary {glossaryId}"
                : glossary.Name,
            SourceDescription = $"Downloaded from Intento glossary {glossaryId}"
        };

        await using var outputStream = interoperableGlossary.ConvertToTbx();
        var fileName = $"{SanitizeFileName(interoperableGlossary.Title)}.tbx";
        var file = await fileManagementClient.UploadAsync(
            outputStream,
            MediaTypeNames.Application.Xml,
            fileName);

        return new DownloadGlossaryResponse
        {
            Glossary = file,
            NumberOfTerms = conceptEntries.Count
        };
    }

    private async Task<GlossaryDto?> GetExistingGlossary(string? glossaryId)
    {
        if (string.IsNullOrWhiteSpace(glossaryId))
            return null;

        var id = ParseGlossaryId(glossaryId);
        var request = new RestRequest($"/ai/text/glossaries/v2/typed/{id}", Method.Get)
            .AddQueryParameter("terms", false);

        return await Client.ExecuteWithErrorHandling<GlossaryDto>(request);
    }

    private async Task<int> CreateGlossary(
        string? name,
        int glossaryType,
        string sourceLanguage,
        string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PluginMisconfigurationException(
                "Glossary name is required when the TBX file does not contain a title.");
        }

        var request = new RestRequest("/ai/text/glossaries/v2/typed", Method.Post);
        var body = JsonConvert.SerializeObject(new
        {
            type = glossaryType,
            cs_type = 5,
            name = name.Trim(),
            origin = "Created by Blackbird",
            language_pairs = new[]
            {
                new { source = sourceLanguage, target = targetLanguage }
            },
            cs_lower = false,
            cs_upper = false,
            cs_regular = true,
            labels = Array.Empty<string>()
        });
        request.AddStringBody(body, RestSharp.ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<GlossaryOperationResponseDto>(request);
        EnsureSuccessfulOperation(response, "create glossary");

        if (response.Id is null or <= 0)
            throw new PluginApplicationException("Intento did not return the created glossary ID.");

        return response.Id.Value;
    }

    private async Task<int> ImportTerms(int glossaryId, List<GlossaryTermPair> terms)
    {
        var request = new RestRequest(
            $"/ai/text/glossaries/v2/typed/import/{glossaryId}",
            Method.Post);
        var body = JsonConvert.SerializeObject(new
        {
            terms = terms.Select(x => new[] { x.Source, x.Target }),
            import_flags = new
            {
                empty_items = "skip",
                duplicated_sources = "fix",
                extra_custom_pairs = "fix",
                missed_predefined = "fix"
            }
        });
        request.AddStringBody(body, RestSharp.ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<GlossaryOperationResponseDto>(request);
        EnsureSuccessfulOperation(response, "import glossary terms");
        return response.Count ?? terms.Count;
    }

    private static List<GlossaryTermPair> ExtractTerms(
        Glossary glossary,
        string sourceLanguage,
        string targetLanguage,
        int glossaryType)
    {
        var result = new List<GlossaryTermPair>();

        foreach (var entry in glossary.ConceptEntries)
        {
            var sourceSection = FindLanguageSection(entry, sourceLanguage);
            var sourceTerm = sourceSection?.Terms.FirstOrDefault()?.Term?.Trim();
            if (string.IsNullOrWhiteSpace(sourceTerm))
                continue;

            if (glossaryType == DntGlossaryType)
            {
                result.Add(new GlossaryTermPair(sourceTerm, sourceTerm));
                continue;
            }

            var targetSection = FindLanguageSection(entry, targetLanguage);
            var targetTerm = targetSection?.Terms.FirstOrDefault()?.Term?.Trim();
            if (string.IsNullOrWhiteSpace(targetTerm))
                continue;

            result.Add(new GlossaryTermPair(sourceTerm, targetTerm));
        }

        return result
            .DistinctBy(x => x.Source, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static GlossaryLanguageSection? FindLanguageSection(
        GlossaryConceptEntry entry,
        string language)
    {
        return entry.LanguageSections.FirstOrDefault(x =>
            string.Equals(
                NormalizeLanguageCode(x.LanguageCode),
                language,
                StringComparison.OrdinalIgnoreCase));
    }

    private static GlossaryConceptEntry CreateConceptEntry(
        int index,
        GlossaryTermValueDto term,
        string sourceLanguage,
        string targetLanguage,
        int glossaryType)
    {
        var sections = new List<GlossaryLanguageSection>
        {
            new(sourceLanguage, [new GlossaryTermSection(term.Source)])
        };

        if (glossaryType != DntGlossaryType && !string.IsNullOrWhiteSpace(term.Target))
        {
            sections.Add(new GlossaryLanguageSection(
                targetLanguage,
                [new GlossaryTermSection(term.Target)]));
        }

        return new GlossaryConceptEntry(index.ToString(), sections);
    }

    private static int ResolveGlossaryType(string? inputType, int? existingType)
    {
        if (string.IsNullOrWhiteSpace(inputType))
            return existingType is DntGlossaryType or UnidirectionalGlossaryType
                ? existingType.Value
                : UnidirectionalGlossaryType;

        if (!int.TryParse(inputType, out var type) ||
            type is not DntGlossaryType and not UnidirectionalGlossaryType)
        {
            throw new PluginMisconfigurationException(
                "Glossary type must be Unidirectional or Do not translate (DNT).");
        }

        return type;
    }

    private static void ValidateExistingGlossary(
        GlossaryDto glossary,
        int glossaryType,
        string sourceLanguage,
        string targetLanguage)
    {
        if (glossary.Type != glossaryType)
        {
            throw new PluginMisconfigurationException(
                $"Glossary {glossary.Id} has type '{FormatGlossaryType(glossary.Type)}', which does not match the selected type '{FormatGlossaryType(glossaryType)}'.");
        }

        var languagePairs = glossary.LanguagePairs
            .Select(x => new
            {
                Source = NormalizeLanguageCode(x.Source),
                Target = NormalizeLanguageCode(x.Target)
            })
            .DistinctBy(x => $"{x.Source}\u001f{x.Target}", StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (languagePairs.Count != 1 ||
            !string.Equals(languagePairs[0].Source, sourceLanguage, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(languagePairs[0].Target, targetLanguage, StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                $"Glossary {glossary.Id} must contain only the selected language pair '{sourceLanguage} -> {targetLanguage}'. Intento terms do not carry locale information, so updating a glossary with another or multiple language pairs could overwrite translations for all pairs.");
        }
    }

    private static string NormalizeRequiredLanguage(string? language, string displayName)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new PluginMisconfigurationException($"{displayName} is required.");

        return NormalizeLanguageCode(language);
    }

    private static string NormalizeLanguageCode(string language) =>
        language.Trim().Replace('_', '-').ToLowerInvariant();

    private static int ParseGlossaryId(string glossaryId)
    {
        if (!int.TryParse(glossaryId, out var id) || id <= 0)
            throw new PluginMisconfigurationException("Glossary ID must be a positive integer.");

        return id;
    }

    private static void EnsureSuccessfulOperation(
        GlossaryOperationResponseDto response,
        string operation)
    {
        if (!string.Equals(response.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginApplicationException(
                $"Intento failed to {operation}: {response.Message ?? "Unknown error"}");
        }
    }

    private static string FormatGlossaryType(int type) => type switch
    {
        DntGlossaryType => "Do not translate (DNT)",
        UnidirectionalGlossaryType => "Unidirectional",
        _ => $"Unknown ({type})"
    };

    private static string NormalizeOutputLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ||
        string.Equals(language, "all", StringComparison.OrdinalIgnoreCase)
            ? "und"
            : language;

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(x => invalidCharacters.Contains(x) ? '_' : x));
    }

    private sealed record GlossaryTermPair(string Source, string Target);
}
