using Apps.Intento.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Intento.Connections;

public class ConnectionValidator(InvocationContext invocationContext) : BaseInvocable(invocationContext), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new IntentoClient(authenticationCredentialsProviders);

            await client.ExecuteWithErrorHandling(
                new RestRequest("/ai/text/detect-language", Method.Get));
        }
        catch (PluginApplicationException ex) when (
            ex.Message.Contains("status 400", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("status 401", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("status 403", StringComparison.OrdinalIgnoreCase))
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
        catch
        {
            return new()
            {
                IsValid = true
            };
        }

        return new()
        {
            IsValid = true
        };
    }
}
