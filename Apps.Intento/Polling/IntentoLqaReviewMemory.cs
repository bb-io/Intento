namespace Apps.Intento.Polling;

public class IntentoLqaReviewMemory
{
    public DateTime? LastPollingTime { get; set; }

    public bool Triggered { get; set; }

    public List<string> ActiveJobIds { get; set; } = [];

    public List<int> RetriedBatchIndexes { get; set; } = [];
}
