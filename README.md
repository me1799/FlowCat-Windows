# 小微Cat（FlowCat Windows）

Windows 网络 / 磁盘监控小工具。托盘常驻，实时显示全局网速，并用 ETW 内核事件按进程统计网络流量与磁盘读写。

## 功能

**网络**
- 全局实时上传 / 下载速度
- 按进程统计网络流量（ETW，TCP/UDP、IPv4/IPv6）
- 多进程应用自动合并到主进程
- 互联网 / 局域网分类与筛选
- 代理 / VPN 进程自动识别分组
- Wi-Fi SSID 与有线 IP 显示
- 域名流量 Top 10

**磁盘**
- 按进程实时读取 / 写入速度
- 只显示活跃进程

**统计**
- 今日 / 昨日 / 本月流量汇总
- 网络、磁盘、域名 Top 10
- CSV 导出（7 天 / 30 天）

**交互**
- 系统托盘 + 可拖动悬浮速度窗
- 主窗口从托盘弹出，失焦隐藏，可固定置顶
- 开机自启
- 单实例保护
- 深色主题

## 系统要求

- Windows 10 1809（17763）及以上 / Windows 11，x64
- 自包含发布，无需安装 .NET 运行时
- 进程级网络监控需要管理员权限

## 使用

1. 以管理员身份运行 `FlowCat.exe`
2. 托盘出现图标，双击打开主窗口
3. 顶部切换「网络 / 磁盘 / 统计」标签页
4. 托盘右键：显示主窗口 / 悬浮窗 / 开机自启 / 退出

## 技术栈

- C# / .NET 8 / WPF
- [WPF-UI](https://github.com/lepoco/wpfui)
- [Microsoft.Diagnostics.Tracing.TraceEvent](https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.TraceEvent)
- [ManagedNativeWifi](https://github.com/emoacht/ManagedNativeWifi)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)

## 项目结构

```
FlowCat/
├── FlowCat.sln
├── src/FlowCat/
│   ├── Models/       # 数据模型
│   ├── Services/     # ETW、DNS、磁盘 I/O、账本、CSV 等
│   ├── ViewModels/
│   └── Views/
└── publish/          # 发布产物
```

## 构建

```bash
dotnet build
dotnet publish src/FlowCat/FlowCat.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

## 许可

MIT