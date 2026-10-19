using System.Windows.Media;

namespace FlowCat.Models;

public class ProcessDiskIo : ViewModels.ViewModelBase
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public ImageSource? Icon { get; set; }

    double _readSpeed;
    double _writeSpeed;
    long _totalRead;
    long _totalWrite;

    public double ReadSpeed
    {
        get => _readSpeed;
        set { if (SetField(ref _readSpeed, value)) OnPropertyChanged(nameof(ReadSpeedDisplay)); }
    }

    public double WriteSpeed
    {
        get => _writeSpeed;
        set { if (SetField(ref _writeSpeed, value)) OnPropertyChanged(nameof(WriteSpeedDisplay)); }
    }

    public long TotalRead
    {
        get => _totalRead;
        set { if (SetField(ref _totalRead, value)) OnPropertyChanged(nameof(TotalReadDisplay)); }
    }

    public long TotalWrite
    {
        get => _totalWrite;
        set { if (SetField(ref _totalWrite, value)) OnPropertyChanged(nameof(TotalWriteDisplay)); }
    }

    public string ReadSpeedDisplay => FormatSpeed(ReadSpeed);
    public string WriteSpeedDisplay => FormatSpeed(WriteSpeed);
    public string TotalReadDisplay => FormatBytes(TotalRead);
    public string TotalWriteDisplay => FormatBytes(TotalWrite);

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
