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
    readonly Dictionary<int, int> _pidToRoot = new();
    readonly Dictionary<int, ProcessTraffic> _rootMap = new();
    readonly Timer _connectionTimer;

    LedgerService? _ledger;
    string _currentSsid = "";

    bool _isAdmin;
    string _statusText = "正在初始化...";
    string _connectionLabel = "";
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

    public string ConnectionLabel
    {
        get => _connectionLabel;
        private set => SetField(ref _connectionLabel, value);
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

        _connectionTimer = new Timer(RefreshConnection, null, Timeout.Infinite, Timeout.Infinite);

        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = FilterProcess;
        ProcessesView.SortDescriptions.Add(
            new SortDescription(nameof(ProcessTraffic.DownloadSpeed), ListSortDirection.Descending));

        ProcessesView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(ProcessTraffic.GroupLabel)));
    }

    public void SetLedger(LedgerService ledger) => _ledger = ledger;

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

        ConnectionLabel = WifiService.GetConnectionLabel();
        _currentSsid = WifiService.GetCurrentSsid() ?? "";
        _connectionTimer.Change(5000, 5000);

        if (_monitor.Start())
            StatusText = "正在监控...";
        else
            StatusText = "ETW 会话启动失败";
    }

    void OnDataUpdated(Dictionary<int, PidTrafficData> snapshot)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var rootAccum = new Dictionary<int, AccumData>();

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
                    existing.Recv += data.Recv;
                    existing.Sent += data.Sent;
                    existing.DlSpeed += data.DlSpeed;
                    existing.UlSpeed += data.UlSpeed;
                    existing.InetRecv += data.InetRecv;
                    existing.InetSent += data.InetSent;
                    existing.LanRecv += data.LanRecv;
                    existing.LanSent += data.LanSent;
                }
                else
                {
                    rootAccum[rootPid] = new AccumData
                    {
                        Recv = data.Recv, Sent = data.Sent,
                        DlSpeed = data.DlSpeed, UlSpeed = data.UlSpeed,
                        InetRecv = data.InetRecv, InetSent = data.InetSent,
                        LanRecv = data.LanRecv, LanSent = data.LanSent
                    };
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

                pt.BytesReceived = data.Recv;
                pt.BytesSent = data.Sent;
                pt.DownloadSpeed = data.DlSpeed;
                pt.UploadSpeed = data.UlSpeed;
                pt.InetBytesReceived = data.InetRecv;
                pt.InetBytesSent = data.InetSent;
                pt.LanBytesReceived = data.LanRecv;
                pt.LanBytesSent = data.LanSent;

                _ledger?.Record(pt.ProcessName, pt.DisplayName,
                    data.Recv, data.Sent,
                    data.InetRecv, data.InetSent,
                    data.LanRecv, data.LanSent,
                    _currentSsid);
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

    void RefreshConnection(object? _)
    {
        var label = WifiService.GetConnectionLabel();
        var ssid = WifiService.GetCurrentSsid() ?? "";
        Application.Current?.Dispatcher.Invoke(() =>
        {
            ConnectionLabel = label;
            _currentSsid = ssid;
        });
    }

    public void Dispose()
    {
        _connectionTimer.Dispose();
        _monitor.DataUpdated -= OnDataUpdated;
        _monitor.Dispose();
    }

    class AccumData
    {
        public long Recv, Sent;
        public double DlSpeed, UlSpeed;
        public long InetRecv, InetSent, LanRecv, LanSent;
    }
}
