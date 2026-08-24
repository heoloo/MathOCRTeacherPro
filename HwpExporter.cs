namespace MathOCRTeacherPro;

public static class HwpExporter
{
    public static bool TryExport(string docxPath, string hwpPath, out string error)
    {
        error = "";
        object? hwp = null;
        try
        {
            var t = Type.GetTypeFromProgID("HWPFrame.HwpObject");
            if (t == null)
            {
                error = "한컴오피스 한글이 설치되어 있지 않거나 COM 자동화를 사용할 수 없습니다.";
                return false;
            }

            hwp = Activator.CreateInstance(t);
            if (hwp == null) throw new Exception("한글 자동화 객체 생성 실패");

            dynamic d = hwp;
            try { d.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule"); } catch { }
            d.Open(docxPath);
            d.SaveAs(hwpPath, "HWP");
            d.Quit();
            return File.Exists(hwpPath);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            try { if (hwp != null) ((dynamic)hwp).Quit(); } catch { }
            return false;
        }
    }
}
