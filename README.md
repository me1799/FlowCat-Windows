# 小微Cat

Windows 网络/磁盘监控工具，实时显示每个进程的网络流量和磁盘读写情况。

## 功能

**网络监控**
- 实时全局上传/下载速度
- 按进程统计网络流量（ETW 内核级采集）
- 区分互联网/局域网流量
- 代理/VPN 软件自动识别分离
- 子进程自动合并到主进程
- Wi-Fi SSID + IP 地址检测（5秒自动刷新）
- DNS 域名流量 Top 10

**磁盘监控**
- 按进程实时读写速度
- 仅显示活跃进程

**统计**
- 今日/昨日/本月网络流量汇总
- 网络/磁盘 Top 10 应用排行
- 域名流量 Top 10（持久化保存）
- CSV 导出（7天/30天）

**其他**
- 系统托盘常驻 + 悬浮速度窗
- 窗口固定（置顶不自动隐藏）
- 开机自启
- 单实例保护
- 深色主题

## 系统要求

- Windows 10 (17763+) / Windows 11
- 管理员权限（进程级监控需要，全局网速不需要）
- .NET 8 运行时（精简版）或零依赖（完整版）

## 使用

1. 以管理员身份运行 `FlowCat.exe`
2. 系统托盘出现 F 图标，双击打开主窗口
3. 右键托盘图标可切换悬浮窗、开机自启、退出

**标签页**
- 网络：实时进程流量，下拉框切换全部/互联网/局域网/代理
- 磁盘：实时进程读写速度
- 统计：流量汇总 + Top 10 排行 + CSV 导出

## 数据存储

```
%APPDATA%\FlowCat\
├── ledger.json      # 流量历史（自动保留90天）
└── proxy-apps.json  # 代理软件列表（可自定义）
```

## 技术栈

- C# / .NET 8 / WPF
- WPF-UI (Fluent Design)
- Microsoft.Diagnostics.Tracing.TraceEvent (ETW)
- ManagedNativeWifi
- Hardcodet.NotifyIcon.Wpf

## 构建

```bash
dotnet build
dotnet publish src/FlowCat/FlowCat.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

## 许可

MIT
