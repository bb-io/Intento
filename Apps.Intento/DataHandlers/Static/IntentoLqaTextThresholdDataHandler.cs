using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.DataHandlers.Static;

public class IntentoLqaTextThresholdDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new DataSourceItem("low", "low | finalize only low"),
            new DataSourceItem("moderate", "moderate | finalize low + moderate"),
            new DataSourceItem("risky", "risky | finalize all")
        ];
    }
}
