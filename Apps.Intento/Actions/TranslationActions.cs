using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Apps.Intento.Service;
using Apps.Intento.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using RestSharp;
using System.Text;
using System.Xml.Linq;

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

        var body = RequestBuilder.BuildSingleTextPayload(
            input.Text,
            input.TargetLanguage,
            input.SourceLanguage,
            input.SmartRouting,
            input.ApplyTranslationStorage,
            input.UpdateTranslationStorage,
            input.DisableNoTrace);

        request.AddStringBody(body, ContentType.Json);

        var response = await client.ExecuteWithErrorHandling<TranslateTextResponseDto>(request);

        return new TranslateTextResponse
        {
            TranslatedText = response.TranslatedText,
            DetectedSourceLanguage = response.DetectedSourceLanguage
        };
    }

    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translate file by using one of the strategies: blackbird or intento")]
    public async Task<TranslateFileResponse> TranslateFile([ActionParameter] TranslateFileRequest input)
    {
        return input.FileTranslationStrategy switch
        {
            "blackbird" => await TranslateFileWithBlackbirdStrategy(input),
            "intento" => await TranslateFileWithNativeStrategy(input),
            _ => throw new PluginMisconfigurationException("Unsupported file translation strategy")
        };
    }

    private async Task<TranslateFileResponse> TranslateFileWithBlackbirdStrategy(TranslateFileRequest input)
    {
        try
        {
            using var stream = await fileManagement.DownloadAsync(input.File);
            var content = await Transformation.Parse(stream, input.File.Name);

            return await HandleInteroperableTransformation(content, input);
        }
        catch (Exception e) when (e.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginMisconfigurationException(
                "The file format is not supported by the Blackbird interoperable strategy.");
        }
    }

    private async Task<TranslateFileResponse> HandleInteroperableTransformation(
        Transformation content,
        TranslateFileRequest input)
    {
        if (!string.IsNullOrWhiteSpace(input.SourceLanguage))
            content.SourceLanguage = input.SourceLanguage;

        if (!string.IsNullOrWhiteSpace(input.TargetLanguage))
            content.TargetLanguage = input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage) || string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException("Source or target language not defined.");

        var overwriteExistingTargets = true;

        bool SegmentFilter(Segment s)
        {
            if (string.IsNullOrWhiteSpace(LineElementMapper.RenderLine(s.Source)))
                return false;

            var isInitial = s.State == null || s.State == SegmentState.Initial;
            if (!isInitial)
                return false;

            if (!overwriteExistingTargets)
            {
                var target = LineElementMapper.RenderLine(s.Target);
                if (!string.IsNullOrWhiteSpace(target))
                    return false;
            }

            return true;
        }

        var units = content.GetUnits()
            .Where(u => u?.Name != null)
            .ToList();

        if (!units.SelectMany(u => u.Segments).Any(SegmentFilter))
            return await BuildFileResponseByFormat(content, input);

        var processed = await units
            .Batch(batchSize: 25, segmentFilter: SegmentFilter)
            .Process<string>(async batch =>
            {
                var sourceTexts = batch
                    .Select(x => LineElementMapper.RenderLine(x.Segment.Source))
                    .ToList();

                var translatedTexts = await TranslateBatchViaTextEndpoint(
                    input,
                    content.SourceLanguage!,
                    content.TargetLanguage!,
                    sourceTexts);

                if (translatedTexts.Count != sourceTexts.Count)
                {
                    translatedTexts = translatedTexts
                        .Take(sourceTexts.Count)
                        .Concat(Enumerable.Repeat(string.Empty, Math.Max(0, sourceTexts.Count - translatedTexts.Count)))
                        .ToList();
                }

                return translatedTexts;
            });

        foreach ((Unit Unit, IEnumerable<(Segment Segment, string Result)> Results) item in processed)
        {
            foreach ((Segment Segment, string Result) r in item.Results)
            {
                if (string.IsNullOrWhiteSpace(r.Result))
                    continue;

                r.Segment.Target = LineElementMapper.MakeLine(r.Result);
            }
        }

        return await BuildFileResponseByFormat(content, input);
    }

    private async Task<TranslateFileResponse> TranslateFileWithNativeStrategy(TranslateFileRequest input)
    {
        using var stream = await fileManagement.DownloadAsync(input.File);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var fileBytes = memoryStream.ToArray();
        if (fileBytes.Length == 0)
            throw new PluginApplicationException("The uploaded file is empty.");

        var format = GetNativeIntentoFormat(input.File.Name, fileBytes);

        if (format == null)
            return await TranslateFileWithBlackbirdStrategy(input);

        var fileContent = format.IsBinary
            ? Convert.ToBase64String(fileBytes)
            : ReadFileContent(fileBytes);

        if (string.IsNullOrWhiteSpace(fileContent))
            throw new PluginApplicationException("The uploaded file is empty.");

        var request = new RestRequest("/ai/text/translate", Method.Post);

        var body = RequestBuilder.BuildNativeFilePayload(
            fileContent,
            input.TargetLanguage,
            input.SourceLanguage,
            format.ApiFormat,
            input.SmartRouting,
            input.ApplyTranslationStorage,
            input.UpdateTranslationStorage,
            input.DisableNoTrace);

        request.AddStringBody(body, ContentType.Json);

        var operation = await Client.ExecuteWithErrorHandling<OperationCreatedResponseDto>(request);

        if (string.IsNullOrWhiteSpace(operation.Id))
            throw new PluginApplicationException("Intento did not return operation id.");

        var translatedContent = await PollNativeFileOperationResult(operation.Id);
        var contentType = string.IsNullOrWhiteSpace(input.File.ContentType)
            ? "application/octet-stream"
            : input.File.ContentType;

        var outputBytes = format.IsBinary
            ? DecodeBase64FileContent(translatedContent)
            : Encoding.UTF8.GetBytes(translatedContent);

        await using var outputStream = new MemoryStream(outputBytes);

        var uploadedFile = await fileManagement.UploadAsync(
            outputStream,
            contentType,
            input.File.Name);

        return new TranslateFileResponse
        {
            File = uploadedFile
        };
    }

    private async Task<List<string>> TranslateBatchViaTextEndpoint(
        TranslateFileRequest input,
        string sourceLanguage,
        string targetLanguage,
        List<string> sourceTexts)
    {
        var request = new RestRequest("/ai/text/translate", Method.Post);

        var body = RequestBuilder.BuildBatchTextPayload(
            sourceTexts,
            targetLanguage,
            sourceLanguage,
            input.SmartRouting,
            input.ApplyTranslationStorage,
            input.UpdateTranslationStorage,
            input.DisableNoTrace);

        request.AddStringBody(body, ContentType.Json);

        var operation = await Client.ExecuteWithErrorHandling<OperationCreatedResponseDto>(request);

        if (string.IsNullOrWhiteSpace(operation.Id))
            throw new PluginApplicationException("Intento did not return operation id.");

        return await PollOperationResult(operation.Id);
    }

    private async Task<TranslateFileResponse> BuildFileResponseByFormat(
        Transformation content,
        TranslateFileRequest input)
    {
        if (input.OutputFileHandling?.Equals("original", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var targetContent = content.Target();
                var outFile = await fileManagement.UploadAsync(
                    targetContent.Serialize().ToStream(),
                    targetContent.OriginalMediaType ?? "application/octet-stream",
                    targetContent.OriginalName ?? input.File.Name);

                return new TranslateFileResponse
                {
                    File = outFile
                };
            }
            catch
            {
                var xliffFallback = await fileManagement.UploadAsync(
                    content.Serialize().ToStream(),
                    MediaTypes.Xliff,
                    content.XliffFileName);

                return new TranslateFileResponse
                {
                    File = xliffFallback
                };
            }
        }

        if (input.OutputFileHandling?.Equals("xliff1", StringComparison.OrdinalIgnoreCase) == true)
        {
            var xliff1String = Xliff1Serializer.Serialize(content);
            var file = await fileManagement.UploadAsync(
                xliff1String.ToStream(),
                MediaTypes.Xliff,
                content.XliffFileName);

            return new TranslateFileResponse
            {
                File = file
            };
        }

        var resultXliff = await fileManagement.UploadAsync(
            content.Serialize().ToStream(),
            MediaTypes.Xliff,
            content.XliffFileName);

        return new TranslateFileResponse
        {
            File = resultXliff
        };
    }

    private async Task<string> PollNativeFileOperationResult(string operationId)
    {
        for (var i = 0; i < 60; i++)
        {
            var request = new RestRequest($"/operations/{operationId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<OperationStatusResponseDto>(request);

            if (status.Done)
            {
                if (status.Error != null)
                    throw new PluginApplicationException($"Intento operation failed: {status.Error}");

                var response = status.Response?.FirstOrDefault();
                var translatedText = response?.Results?.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(translatedText))
                    throw new PluginApplicationException("Intento operation completed but returned empty translated file content.");

                return translatedText;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException("Intento native file translation polling timed out.");
    }

    private static NativeIntentoFormat? GetNativeIntentoFormat(string fileName, byte[] fileBytes)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        var fileContent = extension is ".xlf" or ".xliff"
            ? ReadFileContent(fileBytes)
            : null;

        return extension switch
        {
            ".txt" => new NativeIntentoFormat("text", false),
            ".html" => new NativeIntentoFormat("html", false),
            ".htm" => new NativeIntentoFormat("html", false),
            ".xml" => new NativeIntentoFormat("xml", false),
            ".srt" => new NativeIntentoFormat("srt", false),
            ".icu" => new NativeIntentoFormat("icu", false),
            ".pdf" => new NativeIntentoFormat("pdf", true),
            ".docx" => new NativeIntentoFormat("docx", true),
            ".xlsx" => new NativeIntentoFormat("xlsx", true),
            ".pptx" => new NativeIntentoFormat("pptx", true),
            ".png" => new NativeIntentoFormat("png", true),
            ".jpg" or ".jpeg" => new NativeIntentoFormat("jpeg", true),
            ".bmp" => new NativeIntentoFormat("bmp", true),
            ".zip" => new NativeIntentoFormat("zip", true),
            ".xlf" or ".xliff" when fileContent != null => DetectNativeXliffFormat(fileContent),
            _ => null
        };
    }

    private static string ReadFileContent(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        using var reader = new StreamReader(memoryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static byte[] DecodeBase64FileContent(string translatedContent)
    {
        try
        {
            return Convert.FromBase64String(translatedContent);
        }
        catch (FormatException ex)
        {
            throw new PluginApplicationException($"Intento returned invalid base64 file content: {ex.Message}");
        }
    }

    private static NativeIntentoFormat? DetectNativeXliffFormat(string fileContent)
    {
        try
        {
            var document = XDocument.Parse(fileContent);
            var root = document.Root;

            if (root == null)
                throw new PluginMisconfigurationException("Invalid XLIFF file: root element is missing.");

            var version = root.Attribute("version")?.Value?.Trim();
            var ns = root.Name.NamespaceName;

            if (version == "2.0" || ns.Contains("xliff:document:2.0", StringComparison.OrdinalIgnoreCase))
                return new NativeIntentoFormat("xliff-2.0", false);

            if (version == "2.1" || ns.Contains("xliff:document:2.1", StringComparison.OrdinalIgnoreCase))
                return new NativeIntentoFormat("xliff-2.0", false);

            if (version == "2.2" || ns.Contains("xliff:document:2.2", StringComparison.OrdinalIgnoreCase))
                return new NativeIntentoFormat("xliff-2.0", false);

            return null;
        }
        catch (PluginApplicationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"Failed to detect XLIFF version: {ex.Message}");
        }
    }

    private async Task<List<string>> PollOperationResult(string operationId)
    {
        for (var i = 0; i < 60; i++)
        {
            var request = new RestRequest($"/operations/{operationId}", Method.Get);
            var status = await Client.ExecuteWithErrorHandling<OperationStatusResponseDto>(request);

            if (status.Done)
            {
                if (status.Error != null)
                    throw new PluginApplicationException($"Intento operation failed: {status.Error}");

                var response = status.Response?.FirstOrDefault();
                return response?.Results ?? [];
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new PluginApplicationException("Intento operation polling timed out.");
    }

    private sealed record NativeIntentoFormat(string ApiFormat, bool IsBinary);
}
