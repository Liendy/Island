using System.Text.Json;
using Microsoft.Win32;

namespace AuraIsland.Services;

public sealed class AppSettings
{
    public string City { get; set; } = "London";
    public bool StartWithWindows { get; set; }
    public bool LightTheme { get; set; }

    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraIsland", "settings.json");

    public static AppSettings Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        SetStartup(StartWithWindows);
    }

    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("AuraIsland", $"\\\"{Environment.ProcessPath}\\\"");
        else key.DeleteValue("AuraIsland", false);
    }
}
