using System.Collections.ObjectModel;
using System.Windows;
using FlowCat.Models;
using FlowCat.Services;
using Microsoft.Win32;

namespace FlowCat.ViewModels;

public class DiskRankItem
{
    public string DisplayName { get; set; } = "";
    public string ReadDisplay { get; set; } = "";
    public string WriteDisplay { get; set; } = "";
}

public class NetRankItem
{
    public string DisplayName { get; set; } = "";
    public string RecvDisplay { get; set; } = "";
    public string SentDisplay { get; set; } = "";
}

public class DomainRankItem
{
    public string Domain { get; set; } = "";
    public string BytesDisplay { get; set; } = "";
}

public sealed class StatsViewModel : ViewModelBase, IDisposable
{
    readonly LedgerService _ledger;
    readonly DnsMonitor? _dns;
    readonly System.Threading.Timer _refreshTimer;
    bool _isVisible;

    string _todayRecv = "0 B", _todaySent = "0 B";
    string _yesterdayRecv = "0 B", _yesterdaySent = "0 B";
    string _monthRecv = "0 B", _monthSent = "0 B";
    string _todayDiskRead = "0 B", _todayDiskWrite = "0 B";
    string _yesterdayDiskRead = "0 B", _yesterdayDiskWrite = "0 B";

    public string TodayRecv { get => _todayRecv; private set => SetField(ref _todayRecv, value); }
    public string TodaySent { get => _todaySent; private set => SetField(ref _todaySent, value); }
    public string YesterdayRecv { get => _yesterdayRecv; private set => SetField(ref _yesterdayRecv, value); }
    public string YesterdaySent { get => _yesterdaySent; private set => SetField(ref _yesterdaySent, value); }
    public string MonthRecv { get => _monthRecv; private set => SetField(ref _monthRecv, value); }
    public string MonthSent { get => _monthSent; private set => SetField(ref _monthSent, value); }
    public string TodayDiskRead { get => _todayDiskRead; private set => SetField(ref _todayDiskRead, value); }
    public string TodayDiskWrite { get => _todayDiskWrite; private set => SetField(ref _todayDiskWrite, value); }
    public string YesterdayDiskRead { get => _yesterdayDiskRead; private set => SetField(ref _yesterdayDiskRead, value); }
    public string YesterdayDiskWrite { get => _yesterdayDiskWrite; private set => SetField(ref _yesterdayDiskWrite, value); }

    public ObservableCollection<DiskRankItem> DiskRank { get; } = new();
    public ObservableCollection<NetRankItem> NetRank { get; } = new();
    public ObservableCollection<DomainRankItem> DomainRank { get; } = new();

    public RelayCommand Export7DaysCommand { get; }
    public RelayCommand Export30DaysCommand { get; }

    public StatsViewModel(LedgerService ledger, DnsMonitor? dns = null)
    {
        _ledger = ledger;
        _dns = dns;
        Export7DaysCommand = new RelayCommand(_ => ExportCsv(7));
        Export30DaysCommand = new RelayCommand(_ => ExportCsv(30));
        _refreshTimer = new System.Threading.Timer(_ =>
        {
            if (_isVisible)
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(Refresh);
        }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (visible)
        {
            Refresh();
            _refreshTimer.Change(3000, 3000);
        }
        else
        {
            _refreshTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
    }

    public void Refresh()
    {
        var today = DateTime.Now;
        var todayStr = today.ToString("yyyy-MM-dd");
        var yesterdayStr = today.AddDays(-1).ToString("yyyy-MM-dd");

        var (tRecv, tSent) = _ledger.GetDaySummary(todayStr);
        TodayRecv = FormatBytes(tRecv);
        TodaySent = FormatBytes(tSent);

        var (yRecv, ySent) = _ledger.GetDaySummary(yesterdayStr);
        YesterdayRecv = FormatBytes(yRecv);
        YesterdaySent = FormatBytes(ySent);

        var (mRecv, mSent) = _ledger.GetMonthSummary(today.Year, today.Month);
        MonthRecv = FormatBytes(mRecv);
        MonthSent = FormatBytes(mSent);

        NetRank.Clear();
        var netTop = _ledger.GetDayNetworkTop(todayStr);
        foreach (var r in netTop)
        {
            NetRank.Add(new NetRankItem
            {
                DisplayName = r.DisplayName,
                RecvDisplay = FormatBytes(r.BytesReceived),
                SentDisplay = FormatBytes(r.BytesSent)
            });
        }

        DomainRank.Clear();
        if (_dns != null)
        {
            foreach (var (domain, bytes) in _dns.GetTopDomains(10))
            {
                DomainRank.Add(new DomainRankItem
                {
                    Domain = domain,
                    BytesDisplay = FormatBytes(bytes)
                });
            }
        }

        var (dRead, dWrite) = _ledger.GetDayDiskSummary(todayStr);
        TodayDiskRead = FormatBytes(dRead);
        TodayDiskWrite = FormatBytes(dWrite);

        var (ydRead, ydWrite) = _ledger.GetDayDiskSummary(yesterdayStr);
        YesterdayDiskRead = FormatBytes(ydRead);
        YesterdayDiskWrite = FormatBytes(ydWrite);

        DiskRank.Clear();
        var day = _ledger.GetDay(todayStr);
        if (day != null)
        {
            var top = day.Records
                .Where(r => r.DiskRead > 0 || r.DiskWrite > 0)
                .OrderByDescending(r => r.DiskRead + r.DiskWrite)
                .Take(10);
            foreach (var r in top)
            {
                DiskRank.Add(new DiskRankItem
                {
                    DisplayName = r.DisplayName,
                    ReadDisplay = FormatBytes(r.DiskRead),
                    WriteDisplay = FormatBytes(r.DiskWrite)
                });
            }
        }
    }

    void ExportCsv(int days)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = $"XiaoWeiCat_{days}d_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dlg.ShowDialog() == true)
        {
            var from = DateTime.Now.AddDays(-days);
            var to = DateTime.Now;
            var data = _ledger.GetRange(from, to);
            CsvExporter.Export(dlg.FileName, data);
            MessageBox.Show($"已导出到 {dlg.FileName}", "导出成功");
        }
    }

    public void Dispose() => _refreshTimer.Dispose();

    static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
