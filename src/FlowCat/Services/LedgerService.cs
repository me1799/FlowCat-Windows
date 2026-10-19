using System.IO;
using System.Text.Json;
using FlowCat.Models;

namespace FlowCat.Services;

public sealed class LedgerService : IDisposable
{
    static readonly string LedgerDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowCat");
    static readonly string LedgerPath = Path.Combine(LedgerDir, "ledger.json");
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    readonly Timer _autoSave;
    Ledger _ledger = new();
    readonly object _lock = new();

    public LedgerService()
    {
        Directory.CreateDirectory(LedgerDir);
        Load();
        _autoSave = new Timer(_ => Save(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    void Load()
    {
        try
        {
            if (File.Exists(LedgerPath))
            {
                var json = File.ReadAllText(LedgerPath);
                _ledger = JsonSerializer.Deserialize<Ledger>(json) ?? new Ledger();
            }
        }
        catch { _ledger = new Ledger(); }

        Prune(90);
    }

    void Prune(int keepDays)
    {
        var cutoff = DateTime.Now.AddDays(-keepDays).ToString("yyyy-MM-dd");
        lock (_lock)
            _ledger.Days.RemoveAll(d => string.Compare(d.Date, cutoff, StringComparison.Ordinal) < 0);
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_ledger, JsonOpts);
                File.WriteAllText(LedgerPath, json);
            }
            catch { }
        }
    }

    public void Record(string processName, string displayName,
        long recv, long sent, long inetRecv, long inetSent,
        long lanRecv, long lanSent, string ssid)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            var day = _ledger.Days.FirstOrDefault(d => d.Date == today);
            if (day == null)
            {
                day = new DailyLedger { Date = today };
                _ledger.Days.Add(day);
            }

            var rec = day.Records.FirstOrDefault(r => r.ProcessName == processName);
            if (rec == null)
            {
                rec = new TrafficRecord
                {
                    Date = today,
                    ProcessName = processName,
                    DisplayName = displayName,
                    Ssid = ssid
                };
                day.Records.Add(rec);
            }

            rec.BytesReceived = recv;
            rec.BytesSent = sent;
            rec.InetBytesReceived = inetRecv;
            rec.InetBytesSent = inetSent;
            rec.LanBytesReceived = lanRecv;
            rec.LanBytesSent = lanSent;
            rec.DisplayName = displayName;
            if (!string.IsNullOrEmpty(ssid))
                rec.Ssid = ssid;
        }
    }

    public void RecordDiskIo(string processName, string displayName, long read, long write)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");

        lock (_lock)
        {
            var day = _ledger.Days.FirstOrDefault(d => d.Date == today);
            if (day == null)
            {
                day = new DailyLedger { Date = today };
                _ledger.Days.Add(day);
            }

            var rec = day.Records.FirstOrDefault(r => r.ProcessName == processName);
            if (rec == null)
            {
                rec = new TrafficRecord
                {
                    Date = today,
                    ProcessName = processName,
                    DisplayName = displayName
                };
                day.Records.Add(rec);
            }

            rec.DiskRead = read;
            rec.DiskWrite = write;
            rec.DisplayName = displayName;
        }
    }

    public DailyLedger? GetDay(string date)
    {
        lock (_lock)
            return _ledger.Days.FirstOrDefault(d => d.Date == date);
    }

    public List<DailyLedger> GetRange(DateTime from, DateTime to)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");
        lock (_lock)
            return _ledger.Days
                .Where(d => string.Compare(d.Date, fromStr, StringComparison.Ordinal) >= 0 &&
                            string.Compare(d.Date, toStr, StringComparison.Ordinal) <= 0)
                .OrderByDescending(d => d.Date)
                .ToList();
    }

    public (long totalRecv, long totalSent) GetDaySummary(string date)
    {
        var day = GetDay(date);
        if (day == null) return (0, 0);
        return (day.Records.Sum(r => r.BytesReceived), day.Records.Sum(r => r.BytesSent));
    }

    public List<TrafficRecord> GetDayNetworkTop(string date, int count = 10)
    {
        var day = GetDay(date);
        if (day == null) return new();
        return day.Records
            .Where(r => r.BytesReceived > 0 || r.BytesSent > 0)
            .OrderByDescending(r => r.BytesReceived + r.BytesSent)
            .Take(count)
            .ToList();
    }

    public (long read, long write) GetDayDiskSummary(string date)
    {
        var day = GetDay(date);
        if (day == null) return (0, 0);
        return (day.Records.Sum(r => r.DiskRead), day.Records.Sum(r => r.DiskWrite));
    }

    public (long totalRecv, long totalSent) GetMonthSummary(int year, int month)
    {
        var prefix = $"{year:D4}-{month:D2}";
        lock (_lock)
        {
            var days = _ledger.Days.Where(d => d.Date.StartsWith(prefix));
            long recv = 0, sent = 0;
            foreach (var day in days)
            {
                recv += day.Records.Sum(r => r.BytesReceived);
                sent += day.Records.Sum(r => r.BytesSent);
            }
            return (recv, sent);
        }
    }

    public void Dispose()
    {
        _autoSave.Dispose();
        Save();
    }
}
