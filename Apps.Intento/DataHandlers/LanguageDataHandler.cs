using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.DataHandlers;

public class LanguageDataHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/ai/text/translate/languages", Method.Get);

        var languages = await client.ExecuteWithErrorHandling<List<LanguageDto>>(request);

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();

            languages = languages
                .Where(x =>
                    x.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return languages
            .OrderBy(x => x.Name)
            .Select(x => new DataSourceItem(
                value: x.Code,
                displayName: $"{x.Name} ({x.Code})"));
    }
}
