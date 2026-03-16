using Apps.Appname.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Appname;

public class Invocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Creds =>
        InvocationContext.AuthenticationCredentialsProviders.ToArray();

    protected Client Client { get; }
    public Invocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new(Creds);
    }
}