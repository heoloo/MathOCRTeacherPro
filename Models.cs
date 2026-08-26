namespace MathOCRTeacherPro;

public sealed class RegionItem
{
    public int PageIndex { get; set; }
    public string Kind { get; set; } = "problem";
    public Rectangle Rect { get; set; }
    public string Answer { get; set; } = "";
    public string OcrText { get; set; } = "";
    public string Latex { get; set; } = "";
    public List<OcrSegment> Segments { get; set; } = new();

    // Temporary image files belonging to this problem.
    // Used only during HWP export.
    public List<string> FigureFiles { get; set; } = new();
    public string LayoutType { get; set; } = "normal";
    public string BoxTitle { get; set; } = "";
    public List<string> Choices { get; set; } = new();
}

public sealed class OcrSegment
{
    public string Type { get; set; } = "text";
    public string Content { get; set; } = "";
}
