using Apps.Intento.Actions;
using Apps.Intento.Model.Request;
using Tests.Intento.Base;

namespace Tests.Intento;

[TestClass]
public class UsageActionTests : TestBase
{
    [TestMethod]
    public async Task GetUsage_IsSuccess()
    {
        var action = new UsageActions(InvocationContext, FileManager);

        var result = await action.GetUsageStatistics(new GetUsageStatisticsRequest
        {
            EndpointType = "/usage/intento",
            Bucket = "1day",
            Provider = "ai.text.translate.microsoft.translator_text_api.3-0",
        });

        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

        Assert.IsNotNull(result);
    }
}

