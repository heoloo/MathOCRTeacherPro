using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MathOCRTeacherPro;

public static class HwpExporter
{
    private static readonly Regex MathToken = new(
        @"(?<![가-힣A-Za-z0-9])(" +
        @"\d+\s*(?:<=|>=|≤|≥|<|>)\s*[A-Za-z]\s*(?:<=|>=|≤|≥|<|>)\s*\d+" + // 2≤n≤10
        @"|[A-Za-z]\s*\([A-Za-z0-9+\-]+\)\s*=\s*[^가-힣,?.]+" +             // f(n)=2g(n)
        @"|[A-Za-z]\s*\([A-Za-z0-9+\-]+\)" +                               // f(n)
        @"|[A-Za-z]\s*\^\s*\{?\d+\}?" +                                    // n^2
        @"|[A-Za-z]\s*[²³]" +                                               // n²
        @"|[A-Za-z0-9()]+\s*[+\-]\s*[A-Za-z0-9()]+\s*=\s*[^가-힣,?.]+" +    // expression = ...
        @"|[A-Za-z]" +                                                       // standalone variable
        @")(?![A-Za-z0-9])",
        RegexOptions.Compiled);

    private static void InsertText(dynamic hwp, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        dynamic p = hwp.HParameterSet.HInsertText;
        hwp.HAction.GetDefault("InsertText", p.HSet);
        p.Text = text;
        hwp.HAction.Execute("InsertText", p.HSet);
    }

    private static void NewLine(dynamic hwp) => hwp.HAction.Run("BreakPara");

    private static string ToHwpEquation(string raw)
    {
        var x = raw.Trim()
            .Replace("≤", " <= ")
            .Replace("≥", " >= ")
            .Replace("²", " ^{2}")
            .Replace("³", " ^{3}")
            .Replace("\\le", " <= ")
            .Replace("\\ge", " >= ")
            .Replace("\\times", " times ")
            .Replace("\\cdot", " times ");

        // Convert simple x^2 / x^{2} to explicit HWP equation syntax.
        x = Regex.Replace(x, @"\s*<\s*-\s*", " < -");
        x = Regex.Replace(x, @"\s*>\s*-\s*", " > -");
        x = Regex.Replace(x, @"\s*<=\s*-\s*", " <= -");
        x = Regex.Replace(x, @"\s*>=\s*-\s*", " >= -");
        x = Regex.Replace(x, @"([A-Za-z0-9\)])\s*\^\s*\{?(\d+)\}?", "$1 ^{$2}");
        return x;
    }

    private static bool InsertEquation(dynamic hwp, string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return false;
        script = ToHwpEquation(script);

        try
        {
            dynamic eq = hwp.HParameterSet.HEqEdit;
            hwp.HAction.GetDefault("EquationCreate", eq.HSet);
            eq.String = script;
            eq.BaseUnit = hwp.PointToHwpUnit(10.5);
            try { eq.EqFontName = "HYhwpEQ"; } catch { }

            bool ok = hwp.HAction.Execute("EquationCreate", eq.HSet);
            if (!ok) return false;

            // Make it behave like an inline character.
            try
            {
                hwp.FindCtrl();
                dynamic shape = hwp.HParameterSet.HShapeObject;
                hwp.HAction.GetDefault("EquationPropertyDialog", shape.HSet);
                shape.HSet.SetItem("TreatAsChar", 1);
                try { shape.EqFontName = "HYhwpEQ"; } catch { }
                hwp.HAction.Execute("EquationPropertyDialog", shape.HSet);
                hwp.HAction.Run("Cancel");
            }
            catch { }

            try { hwp.HAction.Run("MoveRight"); } catch { }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Critical v5.2 change:
    // Even when AI mistakenly labels math as ordinary text, detect math-looking
    // spans here and force them through EquationCreate.
    private static void InsertMixedText(dynamic hwp, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var normalized = text.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');

        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            int pos = 0;

            foreach (Match m in MathToken.Matches(line))
            {
                if (m.Index > pos)
                    InsertText(hwp, line.Substring(pos, m.Index - pos));

                if (!InsertEquation(hwp, m.Value))
                    InsertText(hwp, m.Value); // never lose content

                pos = m.Index + m.Length;
            }

            if (pos < line.Length)
                InsertText(hwp, line.Substring(pos));

            if (li < lines.Length - 1)
                NewLine(hwp);
        }
    }


    private static bool InsertFigure(dynamic hwp, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return false;

        try
        {
            NewLine(hwp);

            // Hancom HWP Automation exposes InsertPicture as a high-level method.
            // embedded=true keeps the image inside the HWP file.
            // sizeoption=1 uses the original aspect ratio.
            try
            {
                hwp.InsertPicture(imagePath, true, 1, false, false, 0, 0, 0);
            }
            catch
            {
                // Fallback for HWP versions with a shorter signature.
                try
                {
                    hwp.InsertPicture(imagePath, true, 1);
                }
                catch
                {
                    return false;
                }
            }

            // Center the paragraph containing the figure when possible.
            try
            {
                hwp.HAction.Run("ParagraphShapeAlignCenter");
            }
            catch { }

            NewLine(hwp);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private static void InsertSystemOfEquations(dynamic hwp, List<string> equations)
    {
        if (equations.Count == 0) return;
        string body = string.Join(" # ", equations.Select(ToHwpEquation));
        string script = $"cases {{{body}}}";
        if (!InsertEquation(hwp, script))
        {
            for (int i = 0; i < equations.Count; i++)
            {
                if (i > 0) NewLine(hwp);
                if (!InsertEquation(hwp, equations[i])) InsertText(hwp, equations[i]);
            }
        }
        NewLine(hwp);
    }

    private static void InsertBoxStart(dynamic hwp, string title)
    {
        try
        {
            dynamic p = hwp.HParameterSet.HTableCreation;
            hwp.HAction.GetDefault("TableCreate", p.HSet);
            p.Rows = 1; p.Cols = 1;
            hwp.HAction.Execute("TableCreate", p.HSet);
            if (!string.IsNullOrWhiteSpace(title))
            {
                InsertText(hwp, $"< {title} >");
                NewLine(hwp);
            }
        }
        catch
        {
            InsertText(hwp, "────────────────────────");
            NewLine(hwp);
        }
    }

    private static void InsertBoxEnd(dynamic hwp)
    {
        try { hwp.HAction.Run("MoveDocEnd"); } catch { }
        NewLine(hwp);
    }

    private static void InsertChoices(dynamic hwp, List<string> choices)
    {
        if (choices.Count == 0) return;
        string[] nums={"①","②","③","④","⑤"};
        NewLine(hwp);
        for(int i=0;i<choices.Count;i++)
        {
            InsertText(hwp,(i<nums.Length?nums[i]:$"{i+1}.")+" ");
            if(!InsertEquation(hwp,choices[i])) InsertMixedText(hwp,choices[i]);
            if(choices.Count>=4 && i%2==0 && i<choices.Count-1) InsertText(hwp,"          ");
            else NewLine(hwp);
        }
    }

    private static void InsertProblem(dynamic hwp, RegionItem problem, int number)
    {
        var firstText = problem.Segments.FirstOrDefault(x => x.Type == "text")?.Content ?? "";
        bool hasNumber = Regex.IsMatch(firstText, @"^\s*\d+\s*[\.\)]");
        if (!hasNumber) InsertText(hwp, $"{number}. ");

        if (problem.LayoutType == "system")
        {
            foreach (var seg in problem.Segments.Where(x => x.Type != "equation"))
            {
                if (seg.Type == "newline") NewLine(hwp);
                else InsertMixedText(hwp, seg.Content ?? "");
            }
            var eqs = problem.Segments.Where(x => x.Type=="equation").Select(x=>x.Content).Where(x=>!string.IsNullOrWhiteSpace(x)).ToList();
            InsertSystemOfEquations(hwp, eqs);
        }
        else
        {
            bool boxed = problem.LayoutType == "condition_box";
            if (boxed) InsertBoxStart(hwp, problem.BoxTitle);

            foreach (var seg in problem.Segments)
            {
                if (seg.Type=="equation")
                {
                    if(!InsertEquation(hwp,seg.Content)) InsertMixedText(hwp,seg.Content);
                }
                else if(seg.Type=="newline") NewLine(hwp);
                else InsertMixedText(hwp,seg.Content ?? "");
            }

            if (boxed) InsertBoxEnd(hwp);
        }

        foreach(var figure in problem.FigureFiles) InsertFigure(hwp,figure);
        if(problem.Choices.Count>0) InsertChoices(hwp,problem.Choices);

        if(!string.IsNullOrWhiteSpace(problem.Answer))
        {
            NewLine(hwp); InsertText(hwp,"정답: "); InsertMixedText(hwp,problem.Answer);
        }
        NewLine(hwp); NewLine(hwp);
    }

    public static bool TryCreateMathHwp(string hwpPath, string title, IReadOnlyList<RegionItem> problems, out string error)
    {
        error = "";
        object? obj = null;
        try
        {
            var t = Type.GetTypeFromProgID("HWPFrame.HwpObject");
            if (t == null)
            {
                error = "한컴오피스 한글 Automation을 사용할 수 없습니다.";
                return false;
            }

            obj = Activator.CreateInstance(t);
            if (obj == null) throw new Exception("한글 자동화 객체 생성 실패");
            dynamic hwp = obj;

            try { hwp.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule"); } catch { }
            try { hwp.XHwpWindows.Active_XHwpWindow.Visible = false; } catch { }
            try { hwp.XHwpDocuments.Add(1); } catch { }

            if (!string.IsNullOrWhiteSpace(title))
            {
                InsertText(hwp, title);
                NewLine(hwp);
                NewLine(hwp);
            }

            for (int i = 0; i < problems.Count; i++)
                InsertProblem(hwp, problems[i], i + 1);

            hwp.SaveAs(hwpPath, "HWP");
            try { hwp.Quit(); } catch { }
            return File.Exists(hwpPath);
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            try { if (obj != null) ((dynamic)obj).Quit(); } catch { }
            return false;
        }
        finally
        {
            if (obj != null && Marshal.IsComObject(obj))
                try { Marshal.FinalReleaseComObject(obj); } catch { }
        }
    }

    public static bool TryExport(string docxPath, string hwpPath, out string error)
    {
        error = "";
        object? obj = null;
        try
        {
            var t = Type.GetTypeFromProgID("HWPFrame.HwpObject");
            if (t == null) { error = "한글 Automation 없음"; return false; }
            obj = Activator.CreateInstance(t);
            dynamic hwp = obj!;
            try { hwp.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule"); } catch { }
            hwp.Open(docxPath);
            hwp.SaveAs(hwpPath, "HWP");
            hwp.Quit();
            return File.Exists(hwpPath);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            try { if (obj != null) ((dynamic)obj).Quit(); } catch { }
            return false;
        }
    }
}
