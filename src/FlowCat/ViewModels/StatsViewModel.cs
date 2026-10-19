using System.Windows;
using FlowCat.Services;
using Microsoft.Win32;

namespace FlowCat.ViewModels;

public sealed class StatsViewModel : ViewModelBase
{
    readonly LedgerService _ledger;

    string _todayRecv = "0 B";
    string _todaySent = "0 B";
    string _yesterdayRecv = "0 B";
    string _yesterdaySent = "0 B";
    string _monthRecv = "0 B";
    string _monthSent = "0 B";

    public string TodayRecv { get => _todayRecv; private set => SetField(ref _todayRecv, value); }
    public string TodaySent { get => _todaySent; private set => SetField(ref _todaySent, value); }
    public string YesterdayRecv { get => _yesterdayRecv; private set => SetField(ref _yesterdayRecv, value); }
    public string YesterdaySent { get => _yesterdaySent; private set => SetField(ref _yesterdaySent, value); }
    public string MonthRecv { get => _monthRecv; private set => SetField(ref _monthRecv, value); }
    public string MonthSent { get => _monthSent; private set => SetField(ref _monthSent, value); }

    public RelayCommand ExportCsvCommand { get; }

    public StatsViewModel(LedgerService ledger)
    {
        _ledger = ledger;
        ExportCsvCommand = new RelayCommand(_ => ExportCsv());
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
    }

    void ExportCsv()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = $"FlowCat_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dlg.ShowDialog() == true)
        {
            var from = DateTime.Now.AddDays(-30);
            var to = DateTime.Now;
            var days = _ledger.GetRange(from, to);
            CsvExporter.Export(dlg.FileName, days);
            MessageBox.Show($"已导出到 {dlg.FileName}", "导出成功");
        }
    }

    static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
