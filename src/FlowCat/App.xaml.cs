using System.Diagnostics;
using System.Threading;
using System.Windows;
using Wpf.Ui.Appearance;

namespace FlowCat;

public partial class App : Application
{
    static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "XiaoWeiCat_SingleInstance", out bool isNew);

        if (!isNew)
        {
            KillExisting();
            Thread.Sleep(500);
        }

        base.OnStartup(e);
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    static void KillExisting()
    {
        using var current = Process.GetCurrentProcess();
        var name = current.ProcessName;
        foreach (var proc in Process.GetProcessesByName(name))
        {
            if (proc.Id != current.Id)
            {
                try { proc.Kill(); } catch { }
            }
            proc.Dispose();
        }
    }
}
