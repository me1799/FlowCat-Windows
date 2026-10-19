using System.Net;

namespace FlowCat.Services;

public enum TrafficType
{
    Internet,
    Lan
}

public static class TrafficClassifier
{
    public static TrafficType Classify(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return TrafficType.Lan;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                return TrafficType.Lan;
            return TrafficType.Internet;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length < 4) return TrafficType.Internet;

        // RFC 1918 private ranges
        if (bytes[0] == 10) return TrafficType.Lan;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return TrafficType.Lan;
        if (bytes[0] == 192 && bytes[1] == 168) return TrafficType.Lan;
        // Link-local
        if (bytes[0] == 169 && bytes[1] == 254) return TrafficType.Lan;

        return TrafficType.Internet;
    }

    public static TrafficType Classify(string ipString)
    {
        if (IPAddress.TryParse(ipString, out var address))
            return Classify(address);
        return TrafficType.Internet;
    }
}
