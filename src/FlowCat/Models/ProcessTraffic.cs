using System.Windows.Media;
using FlowCat.Services;

namespace FlowCat.Models;

public class ProcessTraffic : ViewModels.ViewModelBase
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
    public string ExePath { get; set; } = "";
    public ImageSource? Icon { get; set; }
    public ProcessGroup Group { get; set; }

    string _displayName = "";
    long _bytesReceived;
    long _bytesSent;
    double _downloadSpeed;
    double _uploadSpeed;
    long _inetRecv;
    long _inetSent;
    long _lanRecv;
    long _lanSent;

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public long BytesReceived
    {
        get => _bytesReceived;
        set => SetField(ref _bytesReceived, value);
    }

    public long BytesSent
    {
        get => _bytesSent;
        set => SetField(ref _bytesSent, value);
    }

    public double DownloadSpeed
    {
        get => _downloadSpeed;
        set { if (SetField(ref _downloadSpeed, value)) OnPropertyChanged(nameof(DownloadSpeedDisplay)); }
    }

    public double UploadSpeed
    {
        get => _uploadSpeed;
        set { if (SetField(ref _uploadSpeed, value)) OnPropertyChanged(nameof(UploadSpeedDisplay)); }
    }

    public long InetBytesReceived
    {
        get => _inetRecv;
        set { if (SetField(ref _inetRecv, value)) OnPropertyChanged(nameof(InetTotalDisplay)); }
    }

    public long InetBytesSent
    {
        get => _inetSent;
        set { if (SetField(ref _inetSent, value)) OnPropertyChanged(nameof(InetTotalDisplay)); }
    }

    public long LanBytesReceived
    {
        get => _lanRecv;
        set { if (SetField(ref _lanRecv, value)) OnPropertyChanged(nameof(LanTotalDisplay)); }
    }

    public long LanBytesSent
    {
        get => _lanSent;
        set { if (SetField(ref _lanSent, value)) OnPropertyChanged(nameof(LanTotalDisplay)); }
    }

    public string DownloadSpeedDisplay => FormatSpeed(DownloadSpeed);
    public string UploadSpeedDisplay => FormatSpeed(UploadSpeed);
    public string InetTotalDisplay => FormatBytes(InetBytesReceived + InetBytesSent);
    public string LanTotalDisplay => FormatBytes(LanBytesReceived + LanBytesSent);

    public string GroupLabel => Group switch
    {
        ProcessGroup.Proxy => "代理",
        ProcessGroup.System => "系统",
        _ => ""
    };

    static string FormatSpeed(double bytesPerSec) => bytesPerSec switch
    {
        < 1024 => $"{bytesPerSec:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSec / 1024:F1} KB/s",
        < 1024 * 1024 * 1024 => $"{bytesPerSec / (1024 * 1024):F2} MB/s",
        _ => $"{bytesPerSec / (1024 * 1024 * 1024):F2} GB/s"
    };

    static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
