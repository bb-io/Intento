using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.Handlers.Static;

public class FileTranslationStrategyHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new[]
        {
            new DataSourceItem("blackbird", "Blackbird (interoperable translation workflows)"),
            new DataSourceItem("intento", "Intento (native document translation)")
        };
    }
}