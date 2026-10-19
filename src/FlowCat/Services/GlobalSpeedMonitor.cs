using System.Net.NetworkInformation;
using FlowCat.Models;

namespace FlowCat.Services;

public sealed class GlobalSpeedMonitor : IDisposable
{
    readonly Timer _timer;
    SpeedSnapshot? _previous;
    public event Action<SpeedRate>? SpeedUpdated;

    public GlobalSpeedMonitor()
    {
        _timer = new Timer(Sample, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(int intervalMs = 1000)
    {
        _previous = TakeSnapshot();
        _timer.Change(intervalMs, intervalMs);
    }

    public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

    void Sample(object? _)
    {
        var current = TakeSnapshot();
        if (_previous is null) { _previous = current; return; }

        var elapsed = (current.Timestamp - _previous.Timestamp).TotalSeconds;
        if (elapsed <= 0) return;

        var rate = new SpeedRate(
            Math.Max(0, (current.BytesReceived - _previous.BytesReceived) / elapsed),
            Math.Max(0, (current.BytesSent - _previous.BytesSent) / elapsed));

        _previous = current;
        SpeedUpdated?.Invoke(rate);
    }

    static SpeedSnapshot TakeSnapshot()
    {
        long recv = 0, sent = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType is
                NetworkInterfaceType.Loopback or
                NetworkInterfaceType.Tunnel) continue;

            var stats = nic.GetIPStatistics();
            recv += stats.BytesReceived;
            sent += stats.BytesSent;
        }
        return new SpeedSnapshot(recv, sent, DateTime.UtcNow);
    }

    public void Dispose() => _timer.Dispose();
}
