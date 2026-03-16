using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Appname.Actions;

[ActionList]
public class Actions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Action", Description = "Describes the action")]
    public async Task Action()
    {
        
    }
}