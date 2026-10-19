namespace FlowCat.Models;

public class TrafficRecord
{
    public string Date { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    public long InetBytesReceived { get; set; }
    public long InetBytesSent { get; set; }
    public long LanBytesReceived { get; set; }
    public long LanBytesSent { get; set; }
    public string Ssid { get; set; } = "";
}

public class DailyLedger
{
    public string Date { get; set; } = "";
    public List<TrafficRecord> Records { get; set; } = new();
}

public class Ledger
{
    public List<DailyLedger> Days { get; set; } = new();
}
