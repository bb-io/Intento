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
        var numericNote = IntentoLqaEvaluationHelper.FormatNumericScoreNote(0.9234, 0.9);
        var textNote = IntentoLqaEvaluationHelper.FormatTextScoreNote("moderate", "moderate");

        Assert.AreEqual("Intento LQA Score: 0.923 (0.900)", numericNote);
        Assert.AreEqual("Intento LQA Verdict: moderate (threshold: moderate)", textNote);
    }
}
