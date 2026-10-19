using Microsoft.Win32;

namespace FlowCat.Services;

public static class AutoStartService
{
    const string AppName = "FlowCat";
    static readonly string ExePath = Environment.ProcessPath ?? "";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(AppName) is string val &&
                       val.Equals(ExePath, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }

    public static void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue(AppName, ExePath);
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue(AppName, false);
        }
        catch { }
    }

    public static void Toggle()
    {
        if (IsEnabled) Disable();
        else Enable();
    }
}
