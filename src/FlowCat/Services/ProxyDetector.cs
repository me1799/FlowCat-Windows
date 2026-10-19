using System.IO;
using System.Text.Json;

namespace FlowCat.Services;

public enum ProcessGroup
{
    Normal,
    Proxy,
    System
}

public static class ProxyDetector
{
    static readonly HashSet<string> _proxyNames;

    static readonly string[] DefaultProxyNames =
    [
        "clash", "clashforwindows", "clash-verge", "clash-meta", "mihomo",
        "v2ray", "v2rayn", "xray",
        "shadowsocks", "ss-local", "sslocal",
        "trojan", "trojan-go",
        "wireguard",
        "openvpn",
        "sing-box", "singbox",
        "hysteria", "hysteria2",
        "tuic",
        "naiveproxy",
        "nekoray", "nekobox",
        "surfboard",
        "tun2socks",
        "proxifier",
        "hiddify"
    ];

    static readonly HashSet<string> SystemProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost", "system", "idle", "services", "lsass", "csrss",
        "wininit", "smss", "conhost", "runtimebroker", "dwm",
        "searchhost", "startmenuexperiencehost", "shellexperiencehost",
        "taskhostw", "sihost", "fontdrvhost", "ctfmon",
        "securityhealthservice", "securityhealthsystray",
        "spoolsv", "lsaiso", "audiodg", "dashost"
    };

    static ProxyDetector()
    {
        _proxyNames = new HashSet<string>(DefaultProxyNames, StringComparer.OrdinalIgnoreCase);
        LoadCustomList();
    }

    static void LoadCustomList()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FlowCat");
        var configPath = Path.Combine(configDir, "proxy-apps.json");

        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(configDir);
            var json = JsonSerializer.Serialize(DefaultProxyNames, new JsonSerializerOptions { WriteIndented = true });
            try { File.WriteAllText(configPath, json); } catch { }
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var names = JsonSerializer.Deserialize<string[]>(json);
            if (names != null)
            {
                _proxyNames.Clear();
                foreach (var n in names)
                    _proxyNames.Add(n);
            }
        }
        catch { }
    }

    public static ProcessGroup Classify(string processName, string exePath)
    {
        var name = Path.GetFileNameWithoutExtension(processName);

        if (SystemProcessNames.Contains(name))
            return ProcessGroup.System;

        if (_proxyNames.Contains(name))
            return ProcessGroup.Proxy;

        if (!string.IsNullOrEmpty(exePath))
        {
            var exeName = Path.GetFileNameWithoutExtension(exePath);
            if (_proxyNames.Contains(exeName))
                return ProcessGroup.Proxy;
        }

        return ProcessGroup.Normal;
    }
}
