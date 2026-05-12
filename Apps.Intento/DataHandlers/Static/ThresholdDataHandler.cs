using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Globalization;

namespace Apps.Intento.DataHandlers.Static;

public class ThresholdDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new DataSourceItem(0.60f.ToString("0.00", CultureInfo.InvariantCulture), "0.60 | Only flag disasters"),
            new DataSourceItem(0.70f.ToString("0.00", CultureInfo.InvariantCulture), "0.70 | More permissive"),
            new DataSourceItem(0.85f.ToString("0.00", CultureInfo.InvariantCulture), "0.85 | Trust, but verify"),
            new DataSourceItem(0.95f.ToString("0.00", CultureInfo.InvariantCulture), "0.95 | Enterprise-grade caution"),
            new DataSourceItem(1.00f.ToString("0.00", CultureInfo.InvariantCulture), "1.00 | Zero issues")
        ];
    }
}
