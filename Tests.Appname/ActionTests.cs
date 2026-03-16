using Apps.Appname.Actions;
using Tests.Appname.Base;

namespace Tests.Appname;

[TestClass]
public class ActionTests : TestBase
{
    [TestMethod]
    public async Task Dynamic_handler_works()
    {
        var actions = new Actions(InvocationContext);

        await actions.Action();
    }
}
