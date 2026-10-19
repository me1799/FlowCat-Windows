using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowCat.Services;

public static class ProcessResolver
{
    static readonly ConcurrentDictionary<int, ProcessInfo> _cache = new();

    public static ProcessInfo Resolve(int pid)
    {
        return _cache.GetOrAdd(pid, id =>
        {
            try
            {
                using var proc = Process.GetProcessById(id);
                var name = proc.ProcessName;
                string? exePath = null;
                string? displayName = null;
                ImageSource? icon = null;

                try
                {
                    exePath = proc.MainModule?.FileName;
                }
                catch { }

                if (exePath != null)
                {
                    try
                    {
                        var info = FileVersionInfo.GetVersionInfo(exePath);
                        displayName = info.FileDescription;
                    }
                    catch { }

                    try
                    {
                        using var ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                        if (ico != null)
                            icon = ToImageSource(ico);
                    }
                    catch { }
                }

                return new ProcessInfo(
                    name,
                    string.IsNullOrWhiteSpace(displayName) ? name : displayName,
                    exePath ?? "",
                    icon);
            }
            catch
            {
                return new ProcessInfo($"PID {id}", $"PID {id}", "", null);
            }
        });
    }

    public static void Invalidate(int pid) => _cache.TryRemove(pid, out _);

    static ImageSource? ToImageSource(Icon icon)
    {
        try
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        catch { return null; }
    }
}

public record ProcessInfo(
    string ProcessName,
    string DisplayName,
    string ExePath,
    ImageSource? Icon);
