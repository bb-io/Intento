using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.Intento.Model.Response;

public class ReviewTextResponse : IReviewTextOutput
{
    [Display("Score")]
    public float Score { get; set; }

    [Display("Is above threshold")]
    public bool? IsAboveThreshold { get; set; }
}