using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.DataHandlers.Static;

public class IntentoLqaTextThresholdDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new DataSourceItem("low", "low risk | finalize only low risk"),
            new DataSourceItem("moderate", "moderate risk | finalize low + moderate risk"),
            new DataSourceItem("risky", "high risk | finalize all")
        ];
    }
}
