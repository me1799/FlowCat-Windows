using System.Diagnostics;
using Microsoft.Win32;

namespace FlowCat.Services;

public static class AutoStartService
{
    const string TaskName = "FlowCat";
    static readonly string ExePath = Environment.ProcessPath ?? "";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                var (exitCode, _) = RunSchtasks($"/Query /TN \"{TaskName}\" /FO LIST");
                return exitCode == 0;
            }
            catch { return false; }
        }
    }

    public static void Enable()
    {
        RemoveLegacyRegistryEntry();

        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
        RunSchtasks(
            $"/Create /TN \"{TaskName}\" /TR \"\\\"{ExePath}\\\"\" " +
            $"/SC ONLOGON /RU \"{user}\" /RL HIGHEST /F");
    }

    public static void Disable()
    {
        RunSchtasks($"/Delete /TN \"{TaskName}\" /F");
    }

    public static void Toggle()
    {
        if (IsEnabled) Disable();
        else Enable();
    }

    static void RemoveLegacyRegistryEntry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue(TaskName, false);
        }
        catch { }
    }

    static (int ExitCode, string Output) RunSchtasks(string arguments)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        proc.Start();
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(5000);
        return (proc.ExitCode, output);
    }
}
