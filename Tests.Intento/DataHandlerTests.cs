using Apps.Intento.DataHandlers;
using Tests.Intento.Base;

namespace Tests.Intento;

[TestClass]
public class DataHandlerTests : TestBase
{
    [TestMethod]
    public async Task LanguageDataHandler_IsSuccess()
    {
        var handler = new LanguageDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SmartRoutingDataHandler_IsSuccess()
    {
        var handler = new SmartRoutingDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task OcrProviderDataHandler_IsSuccess()
    {
        var handler = new OcrProviderDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SentimentProviderDataHandler_IsSuccess()
    {
        var handler = new SentimentProviderDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task DictionaryProviderDataHandler_IsSuccess()
    {
        var handler = new DictionaryProviderDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task ClassifyProviderDataHandler_IsSuccess()
    {
        var handler = new ClassifyProviderDataHandler(InvocationContext);
        var result = await handler.GetDataAsync(new Blackbird.Applications.Sdk.Common.Dynamic.DataSourceContext
        {
        }, CancellationToken.None);

        foreach (var item in result)
        {
            Console.WriteLine($"Value: {item.Value}, DisplayName: {item.DisplayName}");
        }

        Assert.IsNotNull(result);
    }
}

