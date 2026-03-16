using Apps.Intento.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
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
            var request = new RestRequest("/ai/text/detect-language", Method.Get);

            var response = await client.ExecuteWithErrorHandling(request);

            var isValid = response.IsSuccessful;

            return new ConnectionValidationResponse
            {
                IsValid = isValid,
                Message = isValid
                    ? "Success"
                    : response.Content ?? response.ErrorMessage ?? response.StatusCode.ToString()
            };
        }
        catch (Exception ex)
        {
            return new ConnectionValidationResponse
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}
