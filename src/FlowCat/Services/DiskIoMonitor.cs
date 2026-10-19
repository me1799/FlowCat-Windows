using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlowCat.Services;

public sealed class DiskIoMonitor : IDisposable
{
    readonly ConcurrentDictionary<int, IoSnapshot> _prev = new();
    Thread? _thread;
    volatile bool _running;
    int _intervalMs = 2000;

    public event Action<Dictionary<int, DiskIoData>>? DataUpdated;

    public void Start(int intervalMs = 2000)
    {
        _intervalMs = intervalMs;
        _running = true;
        _thread = new Thread(SampleLoop)
        {
            IsBackground = true,
            Name = "DiskIO-Monitor"
        };
        _thread.Start();
    }

    public void Stop() => _running = false;

    void SampleLoop()
    {
        while (_running)
        {
            try
            {
                var result = new Dictionary<int, DiskIoData>();
                int[] pids;

                try
                {
                    pids = Process.GetProcesses().Select(p => { var id = p.Id; p.Dispose(); return id; }).ToArray();
                }
                catch { Thread.Sleep(_intervalMs); continue; }

                foreach (var pid in pids)
                {
                    if (!_running) break;
                    if (pid == 0 || pid == 4) continue;

                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        if (!GetProcessIoCounters(proc.Handle, out var counters))
                            continue;

                        var read = (long)counters.ReadTransferCount;
                        var write = (long)counters.WriteTransferCount;

                        double readSpeed = 0, writeSpeed = 0;
                        if (_prev.TryGetValue(pid, out var prev))
                        {
                            readSpeed = Math.Max(0, read - prev.Read);
                            writeSpeed = Math.Max(0, write - prev.Write);
                        }

                        _prev[pid] = new IoSnapshot(read, write);

                        if (read > 0 || write > 0)
                            result[pid] = new DiskIoData(read, write, readSpeed, writeSpeed);
                    }
                    catch { }
                }

                if (_running)
                    DataUpdated?.Invoke(result);
            }
            catch { }

            Thread.Sleep(_intervalMs);
        }
    }

    public void Dispose()
    {
        _running = false;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS counters);

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    record IoSnapshot(long Read, long Write);
}

public record DiskIoData(long TotalRead, long TotalWrite, double ReadSpeed, double WriteSpeed);
