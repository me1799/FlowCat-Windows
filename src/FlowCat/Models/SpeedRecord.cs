namespace FlowCat.Models;

public record SpeedSnapshot(
    long BytesReceived,
    long BytesSent,
    DateTime Timestamp);

public record SpeedRate(
    double DownloadBytesPerSec,
    double UploadBytesPerSec)
{
    public string DownloadDisplay => FormatSpeed(DownloadBytesPerSec);
    public string UploadDisplay => FormatSpeed(UploadBytesPerSec);

    static string FormatSpeed(double bytesPerSec) => bytesPerSec switch
    {
        < 1024 => $"{bytesPerSec:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSec / 1024:F1} KB/s",
        < 1024 * 1024 * 1024 => $"{bytesPerSec / (1024 * 1024):F2} MB/s",
        _ => $"{bytesPerSec / (1024 * 1024 * 1024):F2} GB/s"
    };
}
