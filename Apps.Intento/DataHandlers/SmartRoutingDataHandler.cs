using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.DataHandlers;

public class SmartRoutingDataHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/routing-designer/", Method.Get);
        request.AddQueryParameter("visibility", "all");

        var response = await client.ExecuteWithErrorHandling<SmartRoutingResponseDto>(request);
        var routings = response.Data ?? [];

        routings = routings
            .Where(x => x.IsAllowed && x.IsActive)
            .ToList();

        if (!string.IsNullOrWhiteSpace(context.SearchString))
        {
            var search = context.SearchString.Trim();

            routings = routings
                .Where(x =>
                    x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(x.Description) &&
                     x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return routings
            .OrderBy(x => x.Name)
            .Select(x => new DataSourceItem(
                value: x.Name,
                displayName: string.IsNullOrWhiteSpace(x.Description)
                    ? x.Name
                    : $"{x.Name} - {x.Description}"));
    }
}