using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using FlowCat.Models;
using FlowCat.Services;

namespace FlowCat.ViewModels;

public sealed class ProcessListViewModel : ViewModelBase, IDisposable
{
    readonly EtwNetworkMonitor _monitor;
    // PID -> root app PID
    readonly Dictionary<int, int> _pidToRoot = new();
    // Root PID -> merged ProcessTraffic
    readonly Dictionary<int, ProcessTraffic> _rootMap = new();

    bool _isAdmin;
    string _statusText = "正在初始化...";
    bool _showSystem;

    public ObservableCollection<ProcessTraffic> Processes { get; } = new();
    public ICollectionView ProcessesView { get; }

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

    public bool ShowSystem
    {
        get => _showSystem;
        set
        {
            if (SetField(ref _showSystem, value))
                ProcessesView.Refresh();
        }
    }

    public ProcessListViewModel()
    {
        _monitor = new EtwNetworkMonitor();
        _monitor.DataUpdated += OnDataUpdated;

        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = FilterProcess;
        ProcessesView.SortDescriptions.Add(
            new SortDescription(nameof(ProcessTraffic.DownloadSpeed), ListSortDirection.Descending));

        using var groupDesc = ProcessesView.GroupDescriptions as IDisposable;
        ProcessesView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(ProcessTraffic.GroupLabel)));
    }

    bool FilterProcess(object obj)
    {
        if (obj is not ProcessTraffic pt) return false;
        if (!ShowSystem && pt.Group == ProcessGroup.System) return false;
        if (pt.DownloadSpeed == 0 && pt.UploadSpeed == 0 && pt.BytesReceived == 0) return false;
        return true;
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
            // Accumulate per-root-pid
            var rootAccum = new Dictionary<int, (long recv, long sent, double dl, double ul)>();

            foreach (var (pid, data) in snapshot)
            {
                if (pid == 0 || pid == 4) continue;

                if (!_pidToRoot.TryGetValue(pid, out var rootPid))
                {
                    rootPid = ProcessResolver.FindRootAppPid(pid);
                    _pidToRoot[pid] = rootPid;
                }

                if (rootAccum.TryGetValue(rootPid, out var existing))
                {
                    rootAccum[rootPid] = (
                        existing.recv + data.recv,
                        existing.sent + data.sent,
                        existing.dl + data.dlSpeed,
                        existing.ul + data.ulSpeed);
                }
                else
                {
                    rootAccum[rootPid] = (data.recv, data.sent, data.dlSpeed, data.ulSpeed);
                }
            }

            var activeRoots = new HashSet<int>();

            foreach (var (rootPid, data) in rootAccum)
            {
                activeRoots.Add(rootPid);

                if (!_rootMap.TryGetValue(rootPid, out var pt))
                {
                    var info = ProcessResolver.Resolve(rootPid);
                    pt = new ProcessTraffic
                    {
                        ProcessId = rootPid,
                        ProcessName = info.ProcessName,
                        DisplayName = info.DisplayName,
                        ExePath = info.ExePath,
                        Icon = info.Icon,
                        Group = info.Group
                    };
                    _rootMap[rootPid] = pt;
                    Processes.Add(pt);
                }

                pt.BytesReceived = data.recv;
                pt.BytesSent = data.sent;
                pt.DownloadSpeed = data.dl;
                pt.UploadSpeed = data.ul;
            }

            foreach (var pt in Processes)
            {
                if (!activeRoots.Contains(pt.ProcessId))
                {
                    pt.DownloadSpeed = 0;
                    pt.UploadSpeed = 0;
                }
            }

            ProcessesView.Refresh();

            var activeCount = Processes.Count(p => p.DownloadSpeed > 0 || p.UploadSpeed > 0);
            StatusText = $"正在监控 {Processes.Count} 个进程，{activeCount} 个活跃";
        });
    }

    public void Dispose()
    {
        _monitor.DataUpdated -= OnDataUpdated;
        _monitor.Dispose();
    }
}
