//using Apps.Intento.Model.Dto;
//using Apps.Intento.Model.Request;
//using Apps.Intento.Model.Response;
//using Apps.Intento.Service;
//using Blackbird.Applications.Sdk.Common;
//using Blackbird.Applications.Sdk.Common.Actions;
//using Blackbird.Applications.Sdk.Common.Exceptions;
//using Blackbird.Applications.Sdk.Common.Invocation;
//using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
//using Newtonsoft.Json;
//using RestSharp;

//namespace Apps.Intento.Actions;

//[ActionList("Content intelligence")]
//public class ContentIntelligence(InvocationContext invocationContext, IFileManagementClient fileManagement) : IntentoInvocable(invocationContext)
//{
//    [Action("Extract text from an image", Description = "Extract text from an image")]
//    public async Task<OcrImageResponse> OcrImage([ActionParameter] OcrImageRequest input)
//    {
//        using var stream = await fileManagement.DownloadAsync(input.Image);
//        using var memoryStream = new MemoryStream();
//        await stream.CopyToAsync(memoryStream);

//        var bytes = memoryStream.ToArray();
//        if (bytes.Length == 0)
//            throw new PluginMisconfigurationException("The uploaded image is empty.");

//        var base64Image = Convert.ToBase64String(bytes);

//        var request = new RestRequest("/ai/image/ocr", Method.Post);

//        var body = RequestBuilder.BuildOcrImagePayload(
//            base64Image,
//            input.Provider);

//        request.AddStringBody(body, ContentType.Json);

//        var response = await Client.ExecuteWithErrorHandling<OcrImageApiResponse>(request);

//        return new OcrImageResponse
//        {
//            Text = response.Results ?? string.Empty,
//            ProviderId = response.Service?.Provider?.Id
//        };
//    }

//    [Action("Get sentiment of text", Description = "Analyze sentiment of text")]
//    public async Task<GetSentimentOfTextResponse> GetSentimentOfText([ActionParameter] GetSentimentOfTextRequest input)
//    {
//        if (string.IsNullOrWhiteSpace(input.Text))
//            throw new PluginMisconfigurationException("Text is required.");

//        if (string.IsNullOrWhiteSpace(input.Language))
//            throw new PluginMisconfigurationException("Language is required.");

//        var request = new RestRequest("/ai/text/sentiment", Method.Post);

//        var body = RequestBuilder.BuildSentimentPayload(
//            input.Text,
//            input.Language,
//            input.Provider);

//        request.AddStringBody(body, ContentType.Json);

//        var response = await Client.ExecuteWithErrorHandling<SentimentApiResponse>(request);
//        var result = response.Results?.FirstOrDefault();

//        return new GetSentimentOfTextResponse
//        {
//            SentimentLabel = result?.SentimentLabel,
//            SentimentScore = result?.SentimentScore,
//            SentimentConfidence = result?.SentimentConfidence,
//            SentimentSubjectivity = result?.SentimentSubjectivity,
//            Agreement = result?.Agreement,
//            Irony = result?.Irony,
//            ProviderId = response.Service?.Provider?.Id
//        };
//    }

//    [Action("Get meanings of text", Description = "Get dictionary meanings of text")]
//    public async Task<GetMeaningsOfTextResponse> GetMeaningsOfText([ActionParameter] GetMeaningsOfTextRequest input)
//    {
//        if (string.IsNullOrWhiteSpace(input.Text))
//            throw new PluginMisconfigurationException("Text is required.");

//        if (string.IsNullOrWhiteSpace(input.SourceLanguage))
//            throw new PluginMisconfigurationException("Source language is required.");

//        if (string.IsNullOrWhiteSpace(input.TargetLanguage))
//            throw new PluginMisconfigurationException("Target language is required.");

//        var request = new RestRequest("/ai/text/dictionary", Method.Post);

//        var body = RequestBuilder.BuildDictionaryPayload(
//            input.Text,
//            input.SourceLanguage,
//            input.TargetLanguage,
//            input.Provider);

//        request.AddStringBody(body, ContentType.Json);

//        var response = await Client.ExecuteWithErrorHandling<DictionaryApiResponse>(request);

//        return new GetMeaningsOfTextResponse
//        {
//            MeaningsJson = JsonConvert.SerializeObject(response.Results ?? [], Formatting.Indented),
//            ProviderId = response.Service?.Provider?.Id
//        };
//    }

//    [Action("Classify text", Description = "Classify text")]
//    public async Task<ClassifyTextResponse> ClassifyText([ActionParameter] ClassifyTextRequest input)
//    {
//        if (string.IsNullOrWhiteSpace(input.Text))
//            throw new PluginMisconfigurationException("Text is required.");

//        if (string.IsNullOrWhiteSpace(input.Language))
//            throw new PluginMisconfigurationException("Language is required.");

//        var request = new RestRequest("/ai/text/classify", Method.Post);

//        var body = RequestBuilder.BuildClassifyPayload(
//            input.Text,
//            input.Language,
//            input.Provider);

//        request.AddStringBody(body, ContentType.Json);

//        var response = await Client.ExecuteWithErrorHandling<ClassifyResponseDto>(request);

//        return new ClassifyTextResponse
//        {
//            ClassificationJson = response.Results?.ToString(Formatting.Indented) ?? string.Empty,
//            ProviderId = response.Service?.Provider?.Id
//        };
//    }
//}
