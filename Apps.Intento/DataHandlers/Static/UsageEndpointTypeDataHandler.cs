using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.DataHandlers.Static;

public class UsageEndpointTypeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new[]
        {
            new DataSourceItem("intento", "Intento usage"),
            new DataSourceItem("provider", "Provider usage")
        };
    }
}