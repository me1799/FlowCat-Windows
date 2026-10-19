using System.IO;
using System.Text;
using FlowCat.Models;

namespace FlowCat.Services;

public static class CsvExporter
{
    public static void Export(string filePath, IEnumerable<DailyLedger> days)
    {
        var sb = new StringBuilder();
        sb.AppendLine("日期,进程,显示名称,下载(字节),上传(字节),互联网下载,互联网上传,局域网下载,局域网上传,磁盘读取,磁盘写入,SSID");

        foreach (var day in days)
        {
            foreach (var r in day.Records)
            {
                sb.AppendLine(string.Join(",",
                    Escape(r.Date),
                    Escape(r.ProcessName),
                    Escape(r.DisplayName),
                    r.BytesReceived,
                    r.BytesSent,
                    r.InetBytesReceived,
                    r.InetBytesSent,
                    r.LanBytesReceived,
                    r.LanBytesSent,
                    r.DiskRead,
                    r.DiskWrite,
                    Escape(r.Ssid)));
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
