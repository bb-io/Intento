using Newtonsoft.Json;

namespace Apps.Intento.Model.Dto;

public class ScoreOperationStatusResponseDto
{
    [JsonProperty("done")]
    public bool Done { get; set; }

    [JsonProperty("response")]
    public ScoreOperationResponseDto? Response { get; set; }

    [JsonProperty("error")]
    public object? Error { get; set; }
}

public class ScoreOperationResponseDto
{
    [JsonProperty("results")]
    public ScoreResultsDto? Results { get; set; }
}

public class ScoreResultsDto
{
    [JsonProperty("scores")]
    public List<ScoreResultDto>? Scores { get; set; }
}

public class ScoreResultDto
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("value")]
    public object? Value { get; set; }
}