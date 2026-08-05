using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.DataHandlers.Static;

public class GlossaryTypeDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new("1", "Unidirectional"),
        new("2", "Do not translate (DNT)")
    ];
}
