using System.Collections.Concurrent;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace FlowCat.Services;

public sealed class DnsMonitor : IDisposable
{
    const string SessionName = "FlowCatDnsTrace";
    const int MaxEntries = 5000;
    static readonly Guid DnsClientProvider = new("1C95126E-7EEA-49A9-A3FE-A378B03DDB4D");

    TraceEventSession? _session;
    Thread? _thread;

    readonly ConcurrentDictionary<string, string> _ipToDomain = new();
    readonly ConcurrentDictionary<string, long> _domainBytes = new();

    public bool Start()
    {
        try
        {
            try { TraceEventSession.GetActiveSession(SessionName)?.Dispose(); } catch { }

            _session = new TraceEventSession(SessionName) { StopOnDispose = true };
            _session.EnableProvider(DnsClientProvider, TraceEventLevel.Informational);
            _session.Source.Dynamic.All += OnDnsEvent;

            _thread = new Thread(() =>
            {
                try { _session.Source.Process(); } catch { }
            })
            { IsBackground = true, Name = "DNS-Monitor" };
            _thread.Start();
            return true;
        }
        catch { return false; }
    }

    void OnDnsEvent(TraceEvent evt)
    {
        if ((int)evt.ID != 3008) return;

        try
        {
            var queryName = evt.PayloadByName("QueryName")?.ToString();
            var queryResults = evt.PayloadByName("QueryResults")?.ToString();

            if (string.IsNullOrEmpty(queryName) || string.IsNullOrEmpty(queryResults))
                return;

            // Cap IP cache size
            if (_ipToDomain.Count > MaxEntries)
                _ipToDomain.Clear();

            foreach (var ip in queryResults.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = ip.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    _ipToDomain[trimmed] = queryName;
            }
        }
        catch { }
    }

    public void AddDomainBytes(string ip, long bytes)
    {
        if (bytes <= 0) return;
        if (_ipToDomain.TryGetValue(ip, out var domain))
        {
            if (_domainBytes.Count > MaxEntries)
                _domainBytes.Clear();
            _domainBytes.AddOrUpdate(domain, bytes, (_, existing) => existing + bytes);
        }
    }

    public List<(string domain, long bytes)> GetTopDomains(int count = 10)
    {
        return _domainBytes
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    public void Dispose()
    {
        try { _session?.Dispose(); } catch { }
        _session = null;
    }
}
