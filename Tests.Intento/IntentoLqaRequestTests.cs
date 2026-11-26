using Apps.Intento.Model.Request;

namespace Tests.Intento;

[TestClass]
public class IntentoLqaRequestTests
{
    [TestMethod]
    public void ErrorComments_AreEnabledByDefaultForSyncAndBackgroundDownload()
    {
        Assert.AreEqual(true, new ReviewFileWithIntentoLqaRequest().AddErrorComments);
        Assert.AreEqual(true, new DownloadBackgroundIntentoReviewRequest().AddErrorComments);
    }

    [TestMethod]
    public void StateQualifiers_AreAvailableForSyncAndBackgroundStart()
    {
        var qualifiers = new[] { "mt-suggestion" };
        var syncRequest = new ReviewFileWithIntentoLqaRequest { StateQualifiers = qualifiers };
        var backgroundRequest = new ReviewFileWithIntentoLqaBackgroundRequest { StateQualifiers = qualifiers };

        CollectionAssert.AreEqual(qualifiers, syncRequest.StateQualifiers!.ToArray());
        CollectionAssert.AreEqual(qualifiers, backgroundRequest.StateQualifiers!.ToArray());
    }
}
