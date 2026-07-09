using Apps.Intento.Model.Dto;
using System.Globalization;

namespace Apps.Intento.Utils;

public static class IntentoLqaEvaluationHelper
{
    public const string NumericFinalizationMode = "numeric";
    public const string TextFinalizationMode = "text";
    public const string LowRiskBand = "low";
    public const string ModerateRiskBand = "moderate";
    public const string RiskyRiskBand = "risky";
    public const string DefaultTextScoreThreshold = ModerateRiskBand;

    public static IntentoLqaResolvedEvaluation ResolveEvaluation(SearchSegmentEvaluationDto evaluation)
    {
        var rawScore = evaluation.Score ?? evaluation.Details?.FinalScore;

        return new IntentoLqaResolvedEvaluation
        {
            NormalizedScore = rawScore.HasValue ? NormalizeIntentoScore(rawScore.Value) : null,
            ScoreType = NormalizeRiskBand(evaluation.ScoreType)
                ?? NormalizeRiskBand(evaluation.Details?.ScoreType)
        };
    }

    public static string NormalizeTextScoreThreshold(string? threshold)
    {
        return NormalizeRiskBand(threshold) ?? DefaultTextScoreThreshold;
    }

    public static bool IsSupportedTextScoreThreshold(string? threshold)
    {
        return NormalizeRiskBand(threshold) != null;
    }

    public static bool IsEvaluationUsableForMode(IntentoLqaResolvedEvaluation evaluation, string finalizationMode)
    {
        return string.Equals(finalizationMode, TextFinalizationMode, StringComparison.OrdinalIgnoreCase)
            ? !string.IsNullOrWhiteSpace(evaluation.ScoreType)
            : evaluation.NormalizedScore.HasValue;
    }

    public static bool ShouldFinalize(IntentoLqaResolvedEvaluation evaluation, string finalizationMode, double numericThreshold, string textScoreThreshold)
    {
        return string.Equals(finalizationMode, TextFinalizationMode, StringComparison.OrdinalIgnoreCase)
            ? evaluation.ScoreType != null && ShouldFinalizeByTextThreshold(evaluation.ScoreType, textScoreThreshold)
            : evaluation.NormalizedScore.HasValue && evaluation.NormalizedScore.Value >= numericThreshold;
    }

    public static bool ShouldFinalizeByTextThreshold(string scoreType, string threshold)
    {
        var scoreRank = GetRiskBandRank(scoreType);
        var thresholdRank = GetRiskBandRank(threshold);

        return scoreRank <= thresholdRank;
    }

    public static string FormatNumericScoreNote(double score, double threshold)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"Intento LQA Score: {score:F3} ({threshold:F3})");

    public static string FormatTextScoreNote(string scoreType, string threshold)
        => $"Intento LQA Verdict: {scoreType} (threshold: {threshold})";

    public static string FormatVerdictNote(string scoreType)
        => $"Intento LQA Verdict: {scoreType}";

    private static double NormalizeIntentoScore(double score) => score > 1 ? score / 100d : score;

    private static string? NormalizeRiskBand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant() switch
        {
            LowRiskBand => LowRiskBand,
            ModerateRiskBand => ModerateRiskBand,
            RiskyRiskBand => RiskyRiskBand,
            _ => null
        };
    }

    private static int GetRiskBandRank(string value)
    {
        return NormalizeRiskBand(value) switch
        {
            LowRiskBand => 0,
            ModerateRiskBand => 1,
            RiskyRiskBand => 2,
            _ => int.MaxValue
        };
    }
}

public sealed class IntentoLqaResolvedEvaluation
{
    public double? NormalizedScore { get; init; }

    public string? ScoreType { get; init; }
}
