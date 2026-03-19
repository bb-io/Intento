using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.Intento.Model.Response;

public class QualityEstimationResponse : IReviewFileOutput
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Total segments processed")]
    public int TotalSegmentsProcessed { get; set; }

    [Display("Total segments finalized")]
    public int TotalSegmentsFinalized { get; set; }

    [Display("Total segments under threshold")]
    public int TotalSegmentsUnderThreshhold { get; set; }

    [Display("Average metric")]
    public float AverageMetric { get; set; }

    [Display("Percentage segments under threshold")]
    public float PercentageSegmentsUnderThreshhold { get; set; }
}