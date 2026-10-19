using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using FlowCat.Models;
using FlowCat.Services;

namespace FlowCat.ViewModels;

public sealed class DiskIoViewModel : ViewModelBase, IDisposable
{
    readonly DiskIoMonitor _monitor = new();
    readonly Dictionary<int, int> _pidToRoot = new();
    readonly Dictionary<int, ProcessDiskIo> _rootMap = new();

    LedgerService? _ledger;
    string _statusText = "正在初始化...";

    public ObservableCollection<ProcessDiskIo> Processes { get; } = new();
    public ICollectionView ProcessesView { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public DiskIoViewModel()
    {
        _monitor.DataUpdated += OnDataUpdated;

        ProcessesView = CollectionViewSource.GetDefaultView(Processes);
        ProcessesView.Filter = obj => obj is ProcessDiskIo p && (p.ReadSpeed > 0 || p.WriteSpeed > 0 || p.TotalRead > 0);
        ProcessesView.SortDescriptions.Add(
            new SortDescription(nameof(ProcessDiskIo.ReadSpeed), ListSortDirection.Descending));
    }

    public void SetLedger(LedgerService ledger) => _ledger = ledger;

    public void Start()
    {
        _monitor.Start();
        StatusText = "正在监控磁盘 I/O...";
    }

    void OnDataUpdated(Dictionary<int, DiskIoData> snapshot)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var rootAccum = new Dictionary<int, (long read, long write, double rs, double ws)>();

            foreach (var (pid, data) in snapshot)
            {
                if (!_pidToRoot.TryGetValue(pid, out var rootPid))
                {
                    rootPid = ProcessResolver.FindRootAppPid(pid);
                    _pidToRoot[pid] = rootPid;
                }

                if (rootAccum.TryGetValue(rootPid, out var existing))
                {
                    rootAccum[rootPid] = (
                        existing.read + data.TotalRead,
                        existing.write + data.TotalWrite,
                        existing.rs + data.ReadSpeed,
                        existing.ws + data.WriteSpeed);
                }
                else
                {
                    rootAccum[rootPid] = (data.TotalRead, data.TotalWrite, data.ReadSpeed, data.WriteSpeed);
                }
            }

            var activeRoots = new HashSet<int>();

            foreach (var (rootPid, data) in rootAccum)
            {
                activeRoots.Add(rootPid);

                if (!_rootMap.TryGetValue(rootPid, out var p))
                {
                    var info = ProcessResolver.Resolve(rootPid);
                    p = new ProcessDiskIo
                    {
                        ProcessId = rootPid,
                        ProcessName = info.ProcessName,
                        DisplayName = info.DisplayName,
                        Icon = info.Icon
                    };
                    _rootMap[rootPid] = p;
                    Processes.Add(p);
                }

                p.TotalRead = data.read;
                p.TotalWrite = data.write;
                p.ReadSpeed = data.rs;
                p.WriteSpeed = data.ws;

                _ledger?.RecordDiskIo(p.ProcessName, p.DisplayName, data.read, data.write);
            }

            foreach (var p in Processes)
            {
                if (!activeRoots.Contains(p.ProcessId))
                {
                    p.ReadSpeed = 0;
                    p.WriteSpeed = 0;
                }
            }

            ProcessesView.Refresh();

            var activeCount = Processes.Count(p => p.ReadSpeed > 0 || p.WriteSpeed > 0);
            StatusText = $"正在监控 {Processes.Count} 个进程，{activeCount} 个活跃";
        });
    }

    public void Dispose()
    {
        _monitor.DataUpdated -= OnDataUpdated;
        _monitor.Dispose();
    }
}
