using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Intento.DataHandlers.Static;

public class UsageBucketDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new[]
        {
            new DataSourceItem("1min", "1 minute"),
            new DataSourceItem("2mins", "2 minutes"),
            new DataSourceItem("5mins", "5 minutes"),
            new DataSourceItem("10mins", "10 minutes"),
            new DataSourceItem("15mins", "15 minutes"),
            new DataSourceItem("30mins", "30 minutes"),
            new DataSourceItem("1hour", "1 hour"),
            new DataSourceItem("2hours", "2 hours"),
            new DataSourceItem("3hours", "3 hours"),
            new DataSourceItem("6hours", "6 hours"),
            new DataSourceItem("12hours", "12 hours"),
            new DataSourceItem("1day", "1 day")
        };
    }
}