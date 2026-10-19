using System.Collections.Concurrent;
using System.Net;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace FlowCat.Services;

public sealed class EtwNetworkMonitor : IDisposable
{
    const string SessionName = "FlowCatNetTrace";

    TraceEventSession? _session;
    Thread? _processingThread;
    readonly Timer _rateTimer;

    readonly ConcurrentDictionary<int, PidCounters> _counters = new();

    public DnsMonitor? DnsTracker { get; set; }

    public event Action<Dictionary<int, PidTrafficData>>? DataUpdated;
    public bool IsAdmin { get; }

    public EtwNetworkMonitor()
    {
        IsAdmin = IsRunningAsAdmin();
        _rateTimer = new Timer(CalculateRates, null, Timeout.Infinite, Timeout.Infinite);
    }

    public bool Start()
    {
        if (!IsAdmin) return false;

        try
        {
            try { TraceEventSession.GetActiveSession(SessionName)?.Dispose(); } catch { }

            _session = new TraceEventSession(SessionName)
            {
                StopOnDispose = true
            };

            _session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.NetworkTCPIP);

            _session.Source.Kernel.TcpIpRecv += e =>
                AddBytes(e.ProcessID, recv: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.TcpIpSend += e =>
                AddBytes(e.ProcessID, sent: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.UdpIpRecv += e =>
                AddBytes(e.ProcessID, recv: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.UdpIpSend += e =>
                AddBytes(e.ProcessID, sent: e.size, remoteIp: e.daddr);

            _session.Source.Kernel.TcpIpRecvIPV6 += e =>
                AddBytes(e.ProcessID, recv: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.TcpIpSendIPV6 += e =>
                AddBytes(e.ProcessID, sent: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.UdpIpRecvIPV6 += e =>
                AddBytes(e.ProcessID, recv: e.size, remoteIp: e.daddr);
            _session.Source.Kernel.UdpIpSendIPV6 += e =>
                AddBytes(e.ProcessID, sent: e.size, remoteIp: e.daddr);

            _processingThread = new Thread(() =>
            {
                try { _session.Source.Process(); } catch { }
            })
            {
                IsBackground = true,
                Name = "ETW-Network"
            };
            _processingThread.Start();
            _rateTimer.Change(1000, 1000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    void AddBytes(int pid, int recv = 0, int sent = 0, IPAddress? remoteIp = null)
    {
        var c = _counters.GetOrAdd(pid, _ => new PidCounters());
        var isLan = remoteIp != null && TrafficClassifier.Classify(remoteIp) == TrafficType.Lan;

        if (recv > 0)
        {
            Interlocked.Add(ref c.BytesReceived, recv);
            if (isLan) Interlocked.Add(ref c.LanBytesReceived, recv);
            else Interlocked.Add(ref c.InetBytesReceived, recv);
        }
        if (sent > 0)
        {
            Interlocked.Add(ref c.BytesSent, sent);
            if (isLan) Interlocked.Add(ref c.LanBytesSent, sent);
            else Interlocked.Add(ref c.InetBytesSent, sent);
        }

        if (remoteIp != null && !isLan)
            DnsTracker?.AddDomainBytes(remoteIp.ToString(), recv + sent);
    }

    void CalculateRates(object? _)
    {
        var snapshot = new Dictionary<int, PidTrafficData>();

        foreach (var kvp in _counters)
        {
            var c = kvp.Value;
            var recv = Interlocked.Read(ref c.BytesReceived);
            var sent = Interlocked.Read(ref c.BytesSent);
            var dlSpeed = Math.Max(0, recv - c.PrevReceived);
            var ulSpeed = Math.Max(0, sent - c.PrevSent);

            var inetRecv = Interlocked.Read(ref c.InetBytesReceived);
            var inetSent = Interlocked.Read(ref c.InetBytesSent);
            var lanRecv = Interlocked.Read(ref c.LanBytesReceived);
            var lanSent = Interlocked.Read(ref c.LanBytesSent);

            c.PrevReceived = recv;
            c.PrevSent = sent;

            if (recv > 0 || sent > 0)
            {
                snapshot[kvp.Key] = new PidTrafficData(
                    recv, sent, dlSpeed, ulSpeed,
                    inetRecv, inetSent, lanRecv, lanSent);
            }
        }

        DataUpdated?.Invoke(snapshot);
    }

    public void RemoveProcess(int pid) => _counters.TryRemove(pid, out _);

    public void Stop()
    {
        _rateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        try { _session?.Dispose(); } catch { }
        _session = null;
    }

    public void Dispose()
    {
        Stop();
        _rateTimer.Dispose();
    }

    static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    class PidCounters
    {
        public long BytesReceived;
        public long BytesSent;
        public long PrevReceived;
        public long PrevSent;
        public long InetBytesReceived;
        public long InetBytesSent;
        public long LanBytesReceived;
        public long LanBytesSent;
    }
}

public record PidTrafficData(
    long Recv, long Sent, double DlSpeed, double UlSpeed,
    long InetRecv, long InetSent, long LanRecv, long LanSent);
