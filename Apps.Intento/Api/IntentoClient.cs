using Apps.Intento.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Intento.Api;

public class IntentoClient : BlackBirdRestClient
{
    public IntentoClient(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = new Uri("https://api.inten.to"),
    })
    {
        this.AddDefaultHeader("apikey", creds.Get(CredsNames.Token).Value);
        //this.AddDefaultHeader("User-Agent", "Intento.Integration.Blackbird/1.0");
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new Exception($"Could not parse {content} to {typeof(T)}");
        }

        return val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var message = response.Content ?? response.ErrorMessage ?? $"HTTP {(int)response.StatusCode}";

        try
        {
            var json = JToken.Parse(response.Content!);

            var apiErrorMessage = json["error"]?["message"]?.ToString();
            var apiErrorCode = json["error"]?["code"]?.ToString();

            if (!string.IsNullOrWhiteSpace(apiErrorMessage))
            {
                message = string.IsNullOrWhiteSpace(apiErrorCode)
                    ? apiErrorMessage
                    : $"{apiErrorCode}: {apiErrorMessage}";
            }
        }
        catch
        {
        }

        return new PluginApplicationException(message);
    }
}
