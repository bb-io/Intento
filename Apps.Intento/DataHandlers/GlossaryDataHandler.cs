using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.DataHandlers;

public class GlossaryDataHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/ai/text/glossaries/v2/typed", Method.Get);
        var response = await client.ExecuteWithErrorHandling<GlossariesResponseDto>(request);

        var glossaries = response.Glossaries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();
            glossaries = glossaries.Where(x =>
                x.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.LanguagePairs.Any(pair =>
                    pair.Source.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    pair.Target.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        return glossaries
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new DataSourceItem(
                value: x.Id.ToString(),
                displayName: FormatDisplayName(x)));
    }

    private static string FormatDisplayName(GlossaryDto glossary)
    {
        var languagePairs = string.Join(", ", glossary.LanguagePairs.Select(x =>
            $"{x.Source} -> {x.Target}"));
        var name = string.IsNullOrWhiteSpace(glossary.Name)
            ? $"Glossary {glossary.Id}"
            : glossary.Name;

        return string.IsNullOrWhiteSpace(languagePairs)
            ? $"{name} ({glossary.Id})"
            : $"{name} [{languagePairs}] ({glossary.Id})";
    }
}
