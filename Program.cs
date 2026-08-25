using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MathOCRTeacherPro;

internal static class Program
{
    private static string ErrorFile
    {
        get
        {
            try
            {
                var exeDir = AppContext.BaseDirectory;
                return Path.Combine(exeDir, "startup-error.txt");
            }
            catch
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MathOCRTeacherPro");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "startup-error.txt");
            }
        }
    }

    private static void WriteError(string title, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("MathOCR Teacher Pro startup/runtime error");
            sb.AppendLine("========================================");
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Title: {title}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($".NET: {Environment.Version}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"Base directory: {AppContext.BaseDirectory}");
            sb.AppendLine();
            if (ex != null)
            {
                sb.AppendLine(ex.ToString());
                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("INNER EXCEPTION");
                    sb.AppendLine(ex.InnerException.ToString());
                }
            }

            File.WriteAllText(ErrorFile, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MathOCRTeacherPro");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "startup-error.txt"),
                    $"{title}\r\n\r\n{ex}", Encoding.UTF8);
            }
            catch { }
        }
    }

    private static void ShowFatal(string title, Exception ex)
    {
        WriteError(title, ex);
        try
        {
            MessageBox.Show(
                $"MathOCR Teacher Pro를 실행하는 중 오류가 발생했습니다.\r\n\r\n" +
                $"{ex.Message}\r\n\r\n" +
                $"오류 기록 파일:\r\n{ErrorFile}\r\n\r\n" +
                $"startup-error.txt 파일을 ChatGPT에 보내주세요.",
                "MathOCR Teacher Pro - 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { }
    }

    [STAThread]
    static void Main()
    {
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (_, e) =>
            {
                ShowFatal("Application.ThreadException", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception
                         ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown fatal error");
                ShowFatal("AppDomain.UnhandledException", ex);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                WriteError("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            ApplicationConfiguration.Initialize();

            using var form = new MainForm();
            Application.Run(form);
        }
        catch (Exception ex)
        {
            ShowFatal("Program.Main startup failure", ex);
        }
    }
}
