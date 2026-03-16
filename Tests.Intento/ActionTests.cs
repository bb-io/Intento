using Apps.Intento.Actions;
using Tests.Intento.Base;

namespace Tests.Intento;

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
