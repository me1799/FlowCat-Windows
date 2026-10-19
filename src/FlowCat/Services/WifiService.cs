using System.Net.NetworkInformation;

namespace FlowCat.Services;

public static class WifiService
{
    public static string? GetCurrentSsid()
    {
        try
        {
            var ssids = ManagedNativeWifi.NativeWifi.EnumerateConnectedNetworkSsids();
            foreach (var ssid in ssids)
            {
                var name = ssid.ToString();
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
        }
        catch { }

        return null;
    }

    public static string GetConnectionLabel()
    {
        var parts = new List<string>();

        var ssid = GetCurrentSsid();
        if (ssid != null)
            parts.Add($"Wi-Fi: {ssid}");

        if (HasWiredConnection())
            parts.Add("有线");

        if (parts.Count == 0)
            return "未连接";

        return string.Join(" + ", parts);
    }

    static bool HasWiredConnection()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                !nic.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                !nic.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                !nic.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) &&
                !nic.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) &&
                !nic.Description.Contains("Docker", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
