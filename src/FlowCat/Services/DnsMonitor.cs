using System.Collections.Concurrent;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace FlowCat.Services;

public sealed class DnsMonitor : IDisposable
{
    const string SessionName = "FlowCatDnsTrace";
    static readonly Guid DnsClientProvider = new("1C95126E-7EEA-49A9-A3FE-A378B03DDB4D");

    TraceEventSession? _session;
    Thread? _thread;

    // IP string -> domain name
    readonly ConcurrentDictionary<string, string> _ipToDomain = new();
    // domain -> total bytes
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
            {
                IsBackground = true,
                Name = "DNS-Monitor"
            };
            _thread.Start();
            return true;
        }
        catch { return false; }
    }

    void OnDnsEvent(TraceEvent evt)
    {
        // Event ID 3008 = DNS query completed
        if ((int)evt.ID != 3008) return;

        try
        {
            var queryName = evt.PayloadByName("QueryName")?.ToString();
            var queryResults = evt.PayloadByName("QueryResults")?.ToString();

            if (string.IsNullOrEmpty(queryName) || string.IsNullOrEmpty(queryResults))
                return;

            var ips = queryResults.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ip in ips)
            {
                var trimmed = ip.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    _ipToDomain[trimmed] = queryName;
            }
        }
        catch { }
    }

    public string? ResolveDomain(string ip)
    {
        return _ipToDomain.TryGetValue(ip, out var domain) ? domain : null;
    }

    public void AddDomainBytes(string ip, long bytes)
    {
        if (_ipToDomain.TryGetValue(ip, out var domain))
            _domainBytes.AddOrUpdate(domain, bytes, (_, existing) => existing + bytes);
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
