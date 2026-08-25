using System.Runtime.InteropServices;

namespace MathOCRTeacherPro;

public static class HwpExporter
{
    private static void InsertText(dynamic hwp, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        dynamic p = hwp.HParameterSet.HInsertText;
        hwp.HAction.GetDefault("InsertText", p.HSet);
        p.Text = text;
        hwp.HAction.Execute("InsertText", p.HSet);
    }

    private static void NewLine(dynamic hwp) => hwp.HAction.Run("BreakPara");

    private static void InsertEquation(dynamic hwp, string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return;
        script = script.Trim().Replace("\\le","<=").Replace("\\ge",">=").Replace("\\times","times").Replace("\\cdot","times");

        dynamic eq = hwp.HParameterSet.HEqEdit;
        hwp.HAction.GetDefault("EquationCreate", eq.HSet);
        eq.EqFontName = "HYhwpEQ";
        eq.String = script;
        eq.BaseUnit = hwp.PointToHwpUnit(10.5);
        hwp.HAction.Execute("EquationCreate", eq.HSet);

        try
        {
            hwp.FindCtrl();
            dynamic shape = hwp.HParameterSet.HShapeObject;
            hwp.HAction.GetDefault("EquationPropertyDialog", shape.HSet);
            shape.HSet.SetItem("ShapeType", 3);
            shape.Version = "Equation Version 60";
            shape.EqFontName = "HYhwpEQ";
            shape.HSet.SetItem("ApplyTo", 0);
            shape.HSet.SetItem("TreatAsChar", 1);
            hwp.HAction.Execute("EquationPropertyDialog", shape.HSet);
            hwp.HAction.Run("Cancel");
        }
        catch { }
        try { hwp.HAction.Run("MoveRight"); } catch { }
    }

    private static void InsertProblem(dynamic hwp, RegionItem problem, int number)
    {
        var firstText = problem.Segments.FirstOrDefault(x => x.Type == "text")?.Content ?? "";
        bool hasNumber = System.Text.RegularExpressions.Regex.IsMatch(firstText, @"^\s*\d+\s*[\.\)]");
        if (!hasNumber) InsertText(hwp, $"{number}. ");

        foreach (var seg in problem.Segments)
        {
            if (seg.Type == "equation") InsertEquation(hwp, seg.Content);
            else if (seg.Type == "newline") NewLine(hwp);
            else
            {
                var parts=(seg.Content??"").Replace("\r\n","\n").Split('\n');
                for(int i=0;i<parts.Length;i++){ InsertText(hwp,parts[i]); if(i<parts.Length-1) NewLine(hwp); }
            }
        }
        if (!string.IsNullOrWhiteSpace(problem.Answer)) { NewLine(hwp); InsertText(hwp,$"정답: {problem.Answer}"); }
        NewLine(hwp); NewLine(hwp);
    }

    public static bool TryCreateMathHwp(string hwpPath, string title, IReadOnlyList<RegionItem> problems, out string error)
    {
        error=""; object? obj=null;
        try
        {
            var t=Type.GetTypeFromProgID("HWPFrame.HwpObject");
            if(t==null){ error="한컴오피스 한글 Automation을 사용할 수 없습니다."; return false; }
            obj=Activator.CreateInstance(t); if(obj==null) throw new Exception("한글 자동화 객체 생성 실패");
            dynamic hwp=obj;
            try { hwp.RegisterModule("FilePathCheckDLL","FilePathCheckerModule"); } catch { }
            try { hwp.XHwpWindows.Active_XHwpWindow.Visible=false; } catch { }
            try { hwp.XHwpDocuments.Add(1); } catch { }
            if(!string.IsNullOrWhiteSpace(title)){ InsertText(hwp,title); NewLine(hwp); NewLine(hwp); }
            for(int i=0;i<problems.Count;i++) InsertProblem(hwp,problems[i],i+1);
            hwp.SaveAs(hwpPath,"HWP");
            try { hwp.Quit(); } catch { }
            return File.Exists(hwpPath);
        }
        catch(Exception ex){ error=ex.ToString(); try{ if(obj!=null)((dynamic)obj).Quit(); }catch{} return false; }
        finally { if(obj!=null && Marshal.IsComObject(obj)) try{ Marshal.FinalReleaseComObject(obj); }catch{} }
    }

    public static bool TryExport(string docxPath, string hwpPath, out string error)
    {
        error=""; object? obj=null;
        try
        {
            var t=Type.GetTypeFromProgID("HWPFrame.HwpObject"); if(t==null){error="한글 Automation 없음";return false;}
            obj=Activator.CreateInstance(t); dynamic hwp=obj!;
            try { hwp.RegisterModule("FilePathCheckDLL","FilePathCheckerModule"); } catch { }
            hwp.Open(docxPath); hwp.SaveAs(hwpPath,"HWP"); hwp.Quit(); return File.Exists(hwpPath);
        }
        catch(Exception ex){error=ex.Message;try{if(obj!=null)((dynamic)obj).Quit();}catch{}return false;}
    }
}
