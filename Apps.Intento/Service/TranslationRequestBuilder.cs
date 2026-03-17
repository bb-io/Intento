using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Intento.Service;

public static class TranslationRequestBuilder
{
    public static string BuildSingleTextPayload(
        string text,
        string targetLanguage,
        string? sourceLanguage,
        string? smartRouting,
        bool? applyTranslationStorage,
        bool? updateTranslationStorage,
        bool? disableNoTrace)
    {
        var payload = new JObject
        {
            ["context"] = new JObject
            {
                ["text"] = text,
                ["to"] = targetLanguage
            }
        };

        if (!string.IsNullOrWhiteSpace(sourceLanguage))
        {
            payload["context"]!["from"] = sourceLanguage;
        }

        var service = BuildServiceObject(
            smartRouting,
            applyTranslationStorage,
            updateTranslationStorage,
            disableNoTrace,
            isAsync: false);

        if (service != null)
        {
            payload["service"] = service;
        }

        return payload.ToString(Formatting.None);
    }

    public static string BuildReviewTextPayload(
       string sourceText,
       string targetText,
       string targetLanguage,
       string model,
       bool itemize)
    {
        var payload = new JObject
        {
            ["data"] = new JObject
            {
                ["items"] = new JArray(targetText),
                ["reference"] = new JArray(),
                ["source"] = new JArray(sourceText),
                ["lang"] = targetLanguage
            },
            ["scores"] = new JArray(
                new JObject
                {
                    ["name"] = model,
                    ["ignore_errors"] = true
                }),
            ["itemize"] = itemize,
            ["async"] = true
        };

        return payload.ToString(Formatting.None);
    }

    public static string BuildReviewBatchPayload(
        List<string> sourceTexts,
        List<string> targetTexts,
        string targetLanguage,
        string model,
        bool itemize)
    {
        var payload = new JObject
        {
            ["data"] = new JObject
            {
                ["items"] = JArray.FromObject(targetTexts),
                ["reference"] = new JArray(),
                ["source"] = JArray.FromObject(sourceTexts),
                ["lang"] = targetLanguage
            },
            ["scores"] = new JArray(
                new JObject
                {
                    ["name"] = model,
                    ["ignore_errors"] = true
                }),
            ["itemize"] = itemize,
            ["async"] = true
        };

        return payload.ToString(Formatting.None);
    }

    public static string BuildOcrImagePayload(string base64Image, string? provider)
    {
        var payload = new JObject
        {
            ["context"] = new JObject
            {
                ["image"] = base64Image
            }
        };

        if (!string.IsNullOrWhiteSpace(provider))
        {
            payload["service"] = new JObject
            {
                ["provider"] = provider
            };
        }

        return payload.ToString(Formatting.None);
    }

    public static string BuildBatchTextPayload(
        List<string> texts,
        string targetLanguage,
        string? sourceLanguage,
        string? smartRouting,
        bool? applyTranslationStorage,
        bool? updateTranslationStorage,
        bool? disableNoTrace)
    {
        var payload = new JObject
        {
            ["context"] = new JObject
            {
                ["text"] = JArray.FromObject(texts),
                ["to"] = targetLanguage
            }
        };

        if (!string.IsNullOrWhiteSpace(sourceLanguage))
        {
            payload["context"]!["from"] = sourceLanguage;
        }

        var service = BuildServiceObject(
            smartRouting,
            applyTranslationStorage,
            updateTranslationStorage,
            disableNoTrace,
            isAsync: true);

        if (service != null)
        {
            payload["service"] = service;
        }

        return payload.ToString(Formatting.None);
    }

    public static string BuildNativeFilePayload(
        string fileContent,
        string targetLanguage,
        string? sourceLanguage,
        string format,
        string? smartRouting,
        bool? applyTranslationStorage,
        bool? updateTranslationStorage,
        bool? disableNoTrace)
    {
        var payload = new JObject
        {
            ["context"] = new JObject
            {
                ["text"] = fileContent,
                ["to"] = targetLanguage,
                ["format"] = format
            }
        };

        if (!string.IsNullOrWhiteSpace(sourceLanguage))
        {
            payload["context"]!["from"] = sourceLanguage;
        }

        var service = BuildServiceObject(
            smartRouting,
            applyTranslationStorage,
            updateTranslationStorage,
            disableNoTrace,
            isAsync: true);

        if (service != null)
        {
            payload["service"] = service;
        }

        return payload.ToString(Formatting.None);
    }

    public static JObject? BuildServiceObject(
        string? smartRouting,
        bool? applyTranslationStorage,
        bool? updateTranslationStorage,
        bool? disableNoTrace,
        bool isAsync)
    {
        var service = new JObject();

        if (isAsync)
        {
            service["async"] = true;
        }

        if (!string.IsNullOrWhiteSpace(smartRouting))
        {
            service["routing"] = smartRouting;
        }

        if (applyTranslationStorage.HasValue)
        {
            service["translation_storage"] = applyTranslationStorage.Value;
        }

        if (updateTranslationStorage.HasValue)
        {
            service["translation_storage_update"] = updateTranslationStorage.Value;
        }

        if (disableNoTrace.HasValue)
        {
            service["no_trace"] = disableNoTrace.Value;
        }

        return service.HasValues ? service : null;
    }
}

