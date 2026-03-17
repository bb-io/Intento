using Apps.Intento.Actions;
using Tests.Intento.Base;

namespace Tests.Intento;

[TestClass]
public class ReviewActionTests : TestBase
{
    [TestMethod]
    public async Task ReviewText_IsSuccess()
    {
        var action = new ReviewActions(InvocationContext, FileManager);
        var result = await action.ReviewText(new Apps.Intento.Model.Request.ReviewTextRequest
        {
            Model = "labse",
            SourceText = "Hello, world!",
            TargetText = "¡Hola, mundo!",
            TargetLanguage = "es",
            //ScoreThreshold = 0.8
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task ReviewFile_IsSuccess()
    {
        var action = new ReviewActions(InvocationContext, FileManager);
        var result = await action.ReviewFile(new Apps.Intento.Model.Request.ReviewFileRequest
        {
            Model = "labse",
            TargetLanguage = "es",
            //ScoreThreshold = 0.8
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }
}

