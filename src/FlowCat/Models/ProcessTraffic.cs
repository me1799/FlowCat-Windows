using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FlowCat.Models;

public class ProcessTraffic : ViewModels.ViewModelBase
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public string ExePath { get; set; } = "";
    public ImageSource? Icon { get; set; }

    long _bytesReceived;
    long _bytesSent;
    double _downloadSpeed;
    double _uploadSpeed;

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

    public string DownloadSpeedDisplay => FormatSpeed(DownloadSpeed);
    public string UploadSpeedDisplay => FormatSpeed(UploadSpeed);

    static string FormatSpeed(double bytesPerSec) => bytesPerSec switch
    {
        < 1024 => $"{bytesPerSec:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSec / 1024:F1} KB/s",
        < 1024 * 1024 * 1024 => $"{bytesPerSec / (1024 * 1024):F2} MB/s",
        _ => $"{bytesPerSec / (1024 * 1024 * 1024):F2} GB/s"
    };
}
