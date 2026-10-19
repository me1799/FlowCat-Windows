using System.Collections.ObjectModel;
using System.Windows;
using FlowCat.Models;
using FlowCat.Services;

namespace FlowCat.ViewModels;

public sealed class ProcessListViewModel : ViewModelBase, IDisposable
{
    readonly EtwNetworkMonitor _monitor;
    readonly Dictionary<int, ProcessTraffic> _processMap = new();

    bool _isAdmin;
    string _statusText = "正在初始化...";

    public ObservableCollection<ProcessTraffic> Processes { get; } = new();

    public bool IsAdmin
    {
        get => _isAdmin;
        private set => SetField(ref _isAdmin, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public ProcessListViewModel()
    {
        _monitor = new EtwNetworkMonitor();
        _monitor.DataUpdated += OnDataUpdated;
    }

    public void Initialize()
    {
        IsAdmin = _monitor.IsAdmin;
        if (!IsAdmin)
        {
            StatusText = "需要管理员权限才能监控进程流量";
            return;
        }

        if (_monitor.Start())
            StatusText = "正在监控...";
        else
            StatusText = "ETW 会话启动失败";
    }

    void OnDataUpdated(Dictionary<int, (long recv, long sent, double dlSpeed, double ulSpeed)> snapshot)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var activePids = new HashSet<int>();

            foreach (var (pid, data) in snapshot)
            {
                if (pid == 0 || pid == 4) continue;
                activePids.Add(pid);

                if (!_processMap.TryGetValue(pid, out var pt))
                {
                    var info = ProcessResolver.Resolve(pid);
                    pt = new ProcessTraffic
                    {
                        ProcessId = pid,
                        ProcessName = info.ProcessName,
                        DisplayName = info.DisplayName,
                        ExePath = info.ExePath,
                        Icon = info.Icon
                    };
                    _processMap[pid] = pt;
                    Processes.Add(pt);
                }

                pt.BytesReceived = data.recv;
                pt.BytesSent = data.sent;
                pt.DownloadSpeed = data.dlSpeed;
                pt.UploadSpeed = data.ulSpeed;
            }

            // Mark inactive processes with zero speed
            foreach (var pt in Processes)
            {
                if (!activePids.Contains(pt.ProcessId))
                {
                    pt.DownloadSpeed = 0;
                    pt.UploadSpeed = 0;
                }
            }

            StatusText = $"正在监控 {Processes.Count} 个进程";
        });
    }

    public void Dispose()
    {
        _monitor.DataUpdated -= OnDataUpdated;
        _monitor.Dispose();
    }
}
