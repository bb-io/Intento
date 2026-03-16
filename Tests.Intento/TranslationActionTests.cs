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
}

