using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Intento.Actions;

[ActionList("Translation")]
public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : IntentoInvocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Translate text")]
    public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextRequest input)
    {
        var client = new IntentoClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new RestRequest("/ai/text/translate", Method.Post);

        var payload = new TranslateTextRequestDto
        {
            Context = new TranslateTextContextDto
            {
                Text = input.Text,
                TargetLanguage = input.TargetLanguage,
                SourceLanguage = input.SourceLanguage
            }
        };

        var hasServiceSettings =
            !string.IsNullOrWhiteSpace(input.SmartRouting) ||
            input.ApplyTranslationStorage.HasValue ||
            input.UpdateTranslationStorage.HasValue ||
            input.DisableNoTrace.HasValue;

        if (hasServiceSettings)
        {
            payload.Service = new TranslateTextServiceDto
            {
                Routing = input.SmartRouting,
                ApplyTranslationStorage = input.ApplyTranslationStorage,
                UpdateTranslationStorage = input.UpdateTranslationStorage,
                DisableNoTrace = input.DisableNoTrace
            };
        }

        var body = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        request.AddStringBody(body, ContentType.Json);

        var response = await client.ExecuteWithErrorHandling<TranslateTextApiResponse>(request);

        return new TranslateTextResponse
        {
            TranslatedText = response.TranslatedText,
            DetectedSourceLanguage = response.DetectedSourceLanguage
        };
    }
}

