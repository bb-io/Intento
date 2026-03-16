using Apps.Appname.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Appname.Connections;

public class ConnectionValidator(InvocationContext invocationContext) : BaseInvocable(invocationContext), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new Client(authenticationCredentialsProviders);
            var request = new RestRequest();

            var response = await client.ExecuteAsync(request, cancellationToken);

            // Typically you'll want to use the least complex way to validate if a connection is valid.
            var isValid = response.StatusCode != System.Net.HttpStatusCode.Unauthorized;

            return new ConnectionValidationResponse
            {
                IsValid = isValid,
                Message = isValid ? "Success" : (response.Content ?? response.ErrorMessage ?? response.StatusCode.ToString()),
            };

        } 
        catch(Exception ex)
        {
            InvocationContext.Logger?.LogError($"Connection validation failed: {ex.Message}", []);

            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }

    }
}