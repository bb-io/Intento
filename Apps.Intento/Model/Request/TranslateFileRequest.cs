using Apps.Intento.DataHandlers;
using Apps.Intento.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.Intento.Model.Request;

public class TranslateFileRequest : ITranslateFileInput
{
    [Display("File")]
    public FileReference File { get; set; }

    [Display("Target language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string TargetLanguage { get; set; } = string.Empty;

    [Display("Source language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Smart routing")]
    [DataSource(typeof(SmartRoutingDataHandler))]
    public string? SmartRouting { get; set; }

    [Display("Apply translation storage")]
    public bool? ApplyTranslationStorage { get; set; }

    [Display("Update translation storage")]
    public bool? UpdateTranslationStorage { get; set; }

    [Display("Disable no trace")]
    public bool? DisableNoTrace { get; set; }

    [Display("File translation strategy", Description = "Optional. Defaults to Blackbird when empty.")]
    [StaticDataSource(typeof(FileTranslationStrategyHandler))]
    public string? FileTranslationStrategy { get; set; }

    [Display("Output file handling", Description = "original = return original format; otherwise returns XLIFF")]
    [StaticDataSource(typeof(ProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }
}
