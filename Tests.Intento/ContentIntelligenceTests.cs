using Apps.Intento.Actions;
using Tests.Intento.Base;

namespace Tests.Intento;

[TestClass]
public class ContentIntelligenceTests : TestBase
{
    [TestMethod]
    public async Task ContentIntelligence_IsSuccess()
    {
        var action = new ContentIntelligence(InvocationContext, FileManager);
        var result = await action.OcrImage(new  Apps.Intento.Model.Request.OcrImageRequest
        {
            Image = new Blackbird.Applications.Sdk.Common.Files.FileReference
            {
                Name = "test_image.png"
            },
            Provider = "ai.image.ocr.abbyy.cloud_ocr_api"
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }
}

