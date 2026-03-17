using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Apps.Intento.Service;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.Intento.Actions;

[ActionList("Content intelligence")]
public class ContentIntelligence(InvocationContext invocationContext, IFileManagementClient fileManagement) : IntentoInvocable(invocationContext)
{
    [Action("Extract text from an image", Description = "Extract text from an image")]
    public async Task<OcrImageResponse> OcrImage([ActionParameter] OcrImageRequest input)
    {
        using var stream = await fileManagement.DownloadAsync(input.Image);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();
        if (bytes.Length == 0)
            throw new PluginApplicationException("The uploaded image is empty.");

        var base64Image = Convert.ToBase64String(bytes);

        var request = new RestRequest("/ai/image/ocr", Method.Post);

        var body = TranslationRequestBuilder.BuildOcrImagePayload(
            base64Image,
            input.Provider);

        request.AddStringBody(body, ContentType.Json);

        var response = await Client.ExecuteWithErrorHandling<OcrImageApiResponse>(request);

        return new OcrImageResponse
        {
            Text = response.Results ?? string.Empty,
            ProviderId = response.Service?.Provider?.Id
        };
    }
}
