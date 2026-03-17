using Apps.Intento.Api;
using Apps.Intento.Model.Dto;
using Apps.Intento.Model.Request;
using Apps.Intento.Model.Response;
using Apps.Intento.Service;
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

        var body = TranslationRequestBuilder.BuildSingleTextPayload(
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
    [Action("Translate file", Description = "Translate file")]
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

        static string RenderLine(List<LineElement>? line) =>
            line == null || line.Count == 0 ? string.Empty : string.Concat(line.Select(e => e.Render()));

        static List<LineElement> MakeLine(string text) =>
            new() { new LineElement { Value = text } };

        var overwriteExistingTargets = true;

        bool SegmentFilter(Segment s)
        {
            if (string.IsNullOrWhiteSpace(RenderLine(s.Source)))
                return false;

            var isInitial = s.State == null || s.State == SegmentState.Initial;
            if (!isInitial)
                return false;

            if (!overwriteExistingTargets)
            {
                var target = RenderLine(s.Target);
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
                    .Select(x => RenderLine(x.Segment.Source))
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

                r.Segment.Target = MakeLine(r.Result);
            }
        }

        return await BuildFileResponseByFormat(content, input);
    }

    private async Task<TranslateFileResponse> TranslateFileWithNativeStrategy(TranslateFileRequest input)
    {
        var extension = Path.GetExtension(input.File.Name)?.ToLowerInvariant();
        if (extension is ".xlf" or ".xliff")
        {
            return await TranslateFileWithBlackbirdStrategy(input);
        }

        using var stream = await fileManagement.DownloadAsync(input.File);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var fileContent = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fileContent))
            throw new PluginApplicationException("The uploaded file is empty.");

        var format = GetNativeIntentoFormat(input.File.Name, fileContent);

        if (format == null)
            throw new PluginMisconfigurationException(
                "This file format is not supported by Intento native strategy. Supported native formats: TXT, HTML, HTM, XML, CSV. Use Blackbird strategy for XLIFF/XLF and other formats.");

        var request = new RestRequest("/ai/text/translate", Method.Post);

        var body = TranslationRequestBuilder.BuildNativeFilePayload(
            fileContent,
            input.TargetLanguage,
            input.SourceLanguage,
            format,
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

        await using var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(translatedContent));

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

        var body = TranslationRequestBuilder.BuildBatchTextPayload(
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

    private static string? GetNativeIntentoFormat(string fileName, string fileContent)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        return extension switch
        {
            ".txt" => "text",
            ".html" => "html",
            ".htm" => "html",
            ".xml" => "xml",
            ".csv" => "text",
            ".xlf" or ".xliff" => DetectXliffFormat(fileContent),
            _ => null
        };
    }

    private static string DetectXliffFormat(string fileContent)
    {
        try
        {
            var document = XDocument.Parse(fileContent);
            var root = document.Root;

            if (root == null)
                throw new PluginApplicationException("Invalid XLIFF file: root element is missing.");

            var version = root.Attribute("version")?.Value?.Trim();
            var ns = root.Name.NamespaceName;

            if (version == "1.2" || ns.Contains("xliff:document:1.2", StringComparison.OrdinalIgnoreCase))
                return "xliff-1.2";

            if (version == "2.0" || ns.Contains("xliff:document:2.0", StringComparison.OrdinalIgnoreCase))
                return "xliff-2.0";

            if (version == "2.1" || ns.Contains("xliff:document:2.1", StringComparison.OrdinalIgnoreCase))
                return "xliff-2.0";

            if (version == "2.2" || ns.Contains("xliff:document:2.2", StringComparison.OrdinalIgnoreCase))
                return "xliff-2.0";

            throw new PluginApplicationException(
                $"Unsupported XLIFF version. Version='{version}', Namespace='{ns}'.");
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
}