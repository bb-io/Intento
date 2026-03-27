using Apps.Intento.Actions;
using Apps.Intento.Model.Request;
using Tests.Intento.Base;

namespace Tests.Intento;

[TestClass]
public class TranslationActionTests : TestBase
{
    [TestMethod]
    public async Task TranslateText_IsSuccess()
    {
        var action = new TranslationActions(InvocationContext, FileManager);
        var result = await action.TranslateText(new TranslateTextRequest
        {
            Text = "Hello, world!",
            SourceLanguage = "en",
            TargetLanguage = "es",
            ApplyTranslationStorage = true,
            UpdateTranslationStorage = true,
            DisableNoTrace = true
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
        Assert.AreEqual("¡Hola, mundo!", result.TranslatedText);
    }

    [TestMethod]
    public async Task Translate_blackbird_strategy_IsSuccess()
    {
        var action = new TranslationActions(InvocationContext, FileManager);
        var result = await action.TranslateFile(new TranslateFileRequest
        {
            File= new Blackbird.Applications.Sdk.Common.Files.FileReference
            {
                //Name = "taus.xliff"
                Name = "Starting a flight.html"
            },
            FileTranslationStrategy = "blackbird",
            SourceLanguage = "en",
            TargetLanguage = "es",
            ApplyTranslationStorage = true,
            UpdateTranslationStorage = true,
            DisableNoTrace = true,
            OutputFileHandling = "original"
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Translate_intento_strategy_IsSuccess()
    {
        var action = new TranslationActions(InvocationContext, FileManager);
        var result = await action.TranslateFile(new TranslateFileRequest
        {
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference
            {
                Name = "Sample IEP_main.pdf"
                //Name = "Starting a flight.html"
            },
            FileTranslationStrategy = "intento",
            SourceLanguage = "en",
            TargetLanguage = "es",
            ApplyTranslationStorage = true,
            UpdateTranslationStorage = true,
            DisableNoTrace = true,
            //OutputFileHandling = "original"
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }
}

