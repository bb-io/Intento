using Apps.Intento.Actions;
using Apps.Intento.Model.Dto;

namespace Tests.Intento;

[TestClass]
public class GlossarySyncHelperTests
{
    [TestMethod]
    public void GetTermsToDelete_ReturnsOnlyActiveTermsMissingFromImport()
    {
        var existingTerms = new List<GlossaryTermDto>
        {
            CreateTerm("Keep me", "PUBLISHED"),
            CreateTerm("Delete me", "PUBLISHED"),
            CreateTerm("Already deleted", "DELETED"),
            CreateTerm("Duplicate", "PUBLISHED"),
            CreateTerm(" duplicate ", "MODIFIED"),
            new()
        };

        var result = GlossaryActions.GetTermsToDelete(
            existingTerms,
            [" keep ME ", "Imported only"]);

        CollectionAssert.AreEquivalent(
            new[] { "Delete me", "Duplicate" },
            result);
    }

    [TestMethod]
    public void GetTermsToDelete_ReturnsEmptyWhenAllSourcesArePresent()
    {
        var existingTerms = new List<GlossaryTermDto>
        {
            CreateTerm("First", "PUBLISHED"),
            CreateTerm("Second", "ADDED")
        };

        var result = GlossaryActions.GetTermsToDelete(
            existingTerms,
            ["first", "SECOND"]);

        Assert.AreEqual(0, result.Count);
    }

    private static GlossaryTermDto CreateTerm(string source, string status) => new()
    {
        Status = status,
        Term = new GlossaryTermValueDto
        {
            Source = source
        }
    };
}
