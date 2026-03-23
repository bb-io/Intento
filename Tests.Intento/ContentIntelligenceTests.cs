//using Apps.Intento.Actions;
//using Tests.Intento.Base;

//namespace Tests.Intento;

//[TestClass]
//public class ContentIntelligenceTests : TestBase
//{
//    [TestMethod]
//    public async Task OcrImage_IsSuccess()
//    {
//        var action = new ContentIntelligence(InvocationContext, FileManager);
//        var result = await action.OcrImage(new  Apps.Intento.Model.Request.OcrImageRequest
//        {
//            Image = new Blackbird.Applications.Sdk.Common.Files.FileReference
//            {
//                Name = "test_image.png"
//            },
//            Provider = "ai.image.ocr.abbyy.cloud_ocr_api"
//        });
//        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
//        Assert.IsNotNull(result);
//    }

//    [TestMethod]
//    public async Task GetSentimentOfText_IsSuccess()
//    {
//        var action = new ContentIntelligence(InvocationContext, FileManager);
//        var result = await action.GetSentimentOfText(new Apps.Intento.Model.Request.GetSentimentOfTextRequest
//        {
//            Text = "I am very happy today!",
//            Language = "en",
//            Provider= "ai.text.sentiment.microsoft.text_analytics_api.2-0",
            
//        });
//        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
//        Assert.IsNotNull(result);
//    }
//}

