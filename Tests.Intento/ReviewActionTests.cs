using Apps.Intento.Actions;
using Blackbird.Applications.Sdk.Common.Files;
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
            File = new FileReference
            {
                Name = "taus.xliff"
            }
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task IntentoLQA_IsSuccess()
    {
        var action = new ReviewActions(InvocationContext, FileManager);

        var result = await action.ReviewFileWithIntentoLqa(new Apps.Intento.Model.Request.ReviewFileWithIntentoLqaRequest
        {
            SourceLanguage= "en",
            TargetLanguage = "es",
            ScoreThreshold = 0.8,
            File = new FileReference
            {
                //Name = "test_AIQE_Es-en-es-T.mxliff",
                Name = "es_ES_test_aiqe_2.xlsx.xlf"
                //Name = "test_AIQE_Es-en-es-T-source-variant.mxliff"
                //Name = "demo.docx_test.xlf"
            }
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task IntentoLQA_SourceVariant_IsSuccess()
    {
        var action = new ReviewActions(InvocationContext, FileManager);

        var result = await action.ReviewFileWithIntentoLqa(new Apps.Intento.Model.Request.ReviewFileWithIntentoLqaRequest
        {
            TargetLanguage = "es",
            ScoreThreshold = 0.8,
            File = new FileReference
            {
                Name = "test_AIQE_Es-en-es-T-source-variant.mxliff"
            }
        });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        Assert.IsNotNull(result);
    }
}
