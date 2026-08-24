namespace MathOCRTeacherPro;

public sealed class RegionItem
{
    public int PageIndex { get; set; }
    public string Kind { get; set; } = "problem";
    public Rectangle Rect { get; set; }
    public string Answer { get; set; } = "";
    public string OcrText { get; set; } = "";
    public string Latex { get; set; } = "";
}
