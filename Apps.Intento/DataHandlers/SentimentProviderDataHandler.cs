using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.DataHandlers;

public class SentimentProviderDataHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/ai/text/sentiment", Method.Get);

        var providers = await client.ExecuteWithErrorHandling<List<ProviderDto>>(request);

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();
            providers = providers
                .Where(x =>
                    x.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return providers
            .OrderBy(x => x.Name ?? x.Id)
            .Select(x => new DataSourceItem(x.Id, string.IsNullOrWhiteSpace(x.Name) ? x.Id : $"{x.Name} ({x.Id})"));
    }
}