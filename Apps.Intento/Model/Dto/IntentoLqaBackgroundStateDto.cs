namespace Apps.Intento.Model.Dto;

public class IntentoLqaBackgroundStateDto
{
    public string SourceLanguage { get; set; } = string.Empty;

    public string TargetLanguage { get; set; } = string.Empty;

    public double? ScoreThreshold { get; set; }

    public string? TextScoreThreshold { get; set; }

    public bool AddScoreToSegmentComment { get; set; } = true;

    public List<string> JobIds { get; set; } = [];

    public List<string> SearchKeys { get; set; } = [];

    public List<IntentoLqaBackgroundSegmentMappingDto> SegmentMappings { get; set; } = [];
}

public class IntentoLqaBackgroundSegmentMappingDto
{
    public string SearchKey { get; set; } = string.Empty;

    public int UnitIndex { get; set; }

    public int SegmentIndex { get; set; }
}
