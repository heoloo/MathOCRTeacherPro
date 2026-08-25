using System.Text.Json;

namespace MathOCRTeacherPro;

public sealed class AppSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-5.6-luna";
    public bool MakeHwp { get; set; } = true;
    public bool CleanFigures { get; set; } = true;
    public string FigureCleanMode { get; set; } = "Strong";

    public static string Folder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MathOCRTeacherPro");

    public static string FilePath => Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
