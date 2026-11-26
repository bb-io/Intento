using Apps.Intento.Model.Dto;
using Apps.Intento.Utils;

namespace Tests.Intento;

[TestClass]
public class IntentoLqaEvaluationHelperTests
{
    [TestMethod]
    public void ResolveEvaluation_UsesFinalScoreFallbackAndNormalizes()
    {
        var evaluation = new SearchSegmentEvaluationDto
        {
            Details = new SearchSegmentEvaluationDetailsDto
            {
                FinalScore = 92.3,
                ScoreType = "Moderate"
            }
        };

        var result = IntentoLqaEvaluationHelper.ResolveEvaluation(evaluation);

        Assert.AreEqual(0.923, result.NormalizedScore!.Value, 0.0001);
        Assert.AreEqual("moderate", result.ScoreType);
    }

    [TestMethod]
    public void ShouldFinalizeByTextThreshold_AppliesExpectedOrdering()
    {
        Assert.IsTrue(IntentoLqaEvaluationHelper.ShouldFinalizeByTextThreshold("low", "low"));
        Assert.IsTrue(IntentoLqaEvaluationHelper.ShouldFinalizeByTextThreshold("low", "moderate"));
        Assert.IsTrue(IntentoLqaEvaluationHelper.ShouldFinalizeByTextThreshold("moderate", "moderate"));
        Assert.IsFalse(IntentoLqaEvaluationHelper.ShouldFinalizeByTextThreshold("risky", "moderate"));
        Assert.IsTrue(IntentoLqaEvaluationHelper.ShouldFinalizeByTextThreshold("risky", "risky"));
    }

    [TestMethod]
    public void Formatters_UseAgreedText()
    {
        var numericNote = IntentoLqaEvaluationHelper.FormatNumericScoreNote(0.9234);
        var textNote = IntentoLqaEvaluationHelper.FormatTextScoreNote("moderate");
        var verdictNote = IntentoLqaEvaluationHelper.FormatVerdictNote("risky");

        Assert.AreEqual("Intento LQA Score: 0.923", numericNote);
        Assert.AreEqual("Intento LQA Verdict: moderate risk", textNote);
        Assert.AreEqual("Intento LQA Verdict: high risk", verdictNote);
    }

    [TestMethod]
    public void FormatVerdictNote_MapsIntentoBandsToRiskLabels()
    {
        Assert.AreEqual("Intento LQA Verdict: low risk", IntentoLqaEvaluationHelper.FormatVerdictNote("low"));
        Assert.AreEqual("Intento LQA Verdict: moderate risk", IntentoLqaEvaluationHelper.FormatVerdictNote("Moderate"));
        Assert.AreEqual("Intento LQA Verdict: high risk", IntentoLqaEvaluationHelper.FormatVerdictNote("risky"));
    }

    [TestMethod]
    public void StateQualifierFilter_IsOptionalAndCaseInsensitive()
    {
        var filter = IntentoLqaEvaluationHelper.NormalizeStateQualifiers(
            [" mt-suggestion ", "human-translation", "MT-SUGGESTION"]);

        Assert.IsNotNull(filter);
        Assert.AreEqual(2, filter.Count);
        Assert.IsTrue(IntentoLqaEvaluationHelper.MatchesStateQualifier("MT-SUGGESTION", filter));
        Assert.IsFalse(IntentoLqaEvaluationHelper.MatchesStateQualifier("tm-suggestion", filter));
        Assert.IsFalse(IntentoLqaEvaluationHelper.MatchesStateQualifier(null, filter));
        Assert.IsTrue(IntentoLqaEvaluationHelper.MatchesStateQualifier(null, null));
    }
}
