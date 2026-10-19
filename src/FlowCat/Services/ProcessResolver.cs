using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowCat.Services;

public static class ProcessResolver
{
    static readonly ConcurrentDictionary<int, ProcessInfo> _cache = new();
    static readonly ConcurrentDictionary<int, int> _parentCache = new();

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

                try { exePath = proc.MainModule?.FileName; } catch { }

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

                var group = ProxyDetector.Classify(name, exePath ?? "");

                return new ProcessInfo(
                    name,
                    string.IsNullOrWhiteSpace(displayName) ? name : displayName,
                    exePath ?? "",
                    icon,
                    group);
            }
            catch
            {
                return new ProcessInfo($"PID {id}", $"PID {id}", "", null, ProcessGroup.Normal);
            }
        });
    }

    public static int GetParentPid(int pid)
    {
        return _parentCache.GetOrAdd(pid, id =>
        {
            try
            {
                var handle = OpenProcess(0x1000, false, (uint)id);
                if (handle == IntPtr.Zero) return 0;

                try
                {
                    var pbi = new PROCESS_BASIC_INFORMATION();
                    int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
                    if (status == 0)
                        return (int)pbi.InheritedFromUniqueProcessId;
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            catch { }
            return 0;
        });
    }

    public static int FindRootAppPid(int pid)
    {
        var visited = new HashSet<int>();
        var current = pid;

        while (visited.Add(current))
        {
            var parentPid = GetParentPid(current);
            if (parentPid <= 4) break;

            try
            {
                var parentInfo = Resolve(parentPid);
                var childInfo = Resolve(current);

                if (IsSameApp(childInfo.ProcessName, parentInfo.ProcessName))
                {
                    current = parentPid;
                    continue;
                }
            }
            catch { }

            break;
        }

        return current;
    }

    public static void PurgeStale()
    {
        foreach (var pid in _cache.Keys)
        {
            try { Process.GetProcessById(pid); }
            catch { _cache.TryRemove(pid, out _); _parentCache.TryRemove(pid, out _); }
        }
    }

    static bool IsSameApp(string childName, string parentName)
    {
        if (string.Equals(childName, parentName, StringComparison.OrdinalIgnoreCase))
            return true;

        var childBase = StripSuffix(childName);
        var parentBase = StripSuffix(parentName);
        return string.Equals(childBase, parentBase, StringComparison.OrdinalIgnoreCase);
    }

    static string StripSuffix(string name)
    {
        foreach (var suffix in new[] { "helper", "renderer", "gpu", "utility", "crashhandler", "broker" })
        {
            var idx = name.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
            if (idx > 0) return name[..idx].TrimEnd('.', '-', '_', ' ');
        }
        return name;
    }

    static ImageSource? ToImageSource(Icon icon)
    {
        try
        {
            var bmp = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }
}

public record ProcessInfo(
    string ProcessName,
    string DisplayName,
    string ExePath,
    ImageSource? Icon,
    ProcessGroup Group);
