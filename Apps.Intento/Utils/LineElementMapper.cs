using Blackbird.Filters.Transformations;

namespace Apps.Intento.Utils;

public static class LineElementMapper
{
    public static string RenderLine(List<LineElement>? line) =>
        line == null || line.Count == 0 ? string.Empty : string.Concat(line.Select(x => x.Value));

    public static List<LineElement> MakeLine(string text) =>
        [new LineElement { Value = text }];
}
