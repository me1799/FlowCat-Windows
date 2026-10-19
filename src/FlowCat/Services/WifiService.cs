using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
        var wifiIp = GetNicIp(NetworkInterfaceType.Wireless80211);
        if (ssid != null)
        {
            var label = $"Wi-Fi: {ssid}";
            if (wifiIp != null) label += $" ({wifiIp})";
            parts.Add(label);
        }

        var ethernetIp = GetNicIp(NetworkInterfaceType.Ethernet);
        if (ethernetIp != null)
            parts.Add($"有线: {ethernetIp}");

        if (parts.Count == 0)
            return "未连接";

        return string.Join(" | ", parts);
    }

    static string? GetNicIp(NetworkInterfaceType targetType)
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType != targetType) continue;
                if (IsVirtualAdapter(nic)) continue;

                var props = nic.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
                }
            }
        }
        catch { }
        return null;
    }

    static bool IsVirtualAdapter(NetworkInterface nic)
    {
        var desc = nic.Description;
        var name = nic.Name;
        string[] keywords =
        [
            "Virtual", "VMware", "Hyper-V", "VirtualBox", "Docker",
            "vEthernet", "WSL", "Tailscale", "ZeroTier", "Hamachi",
            "TAP-", "TUN", "WireGuard", "Npcap", "Loopback"
        ];
        foreach (var kw in keywords)
        {
            if (desc.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static bool HasWiredConnection()
    {
        return GetNicIp(NetworkInterfaceType.Ethernet) != null;
    }
}
