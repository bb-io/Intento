using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.DataHandlers;

public class StorageActionDataHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/storage/action/list", Method.Get);

        var response = await client.ExecuteWithErrorHandling<StorageActionListResponseDto>(request);
        var actions = response.Actions ?? [];

        actions = actions
            .Where(x => string.Equals(x.Type, "evaluation", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();
            actions = actions
                .Where(x =>
                    x.ActionId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.Description) && x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return actions
            .OrderBy(x => x.Name ?? x.ActionId)
            .Select(x => new DataSourceItem(
                x.ActionId,
                string.IsNullOrWhiteSpace(x.Name)
                    ? x.ActionId
                    : $"{x.Name} ({x.ActionId})"));
    }
}
