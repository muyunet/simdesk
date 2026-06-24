# SimDesk 架构文档

## 概述

SimDesk 是一个 Windows 桌面整理工具，使用 C# (.NET 10) + WPF 开发。无主窗口，仅有系统托盘图标和桌面悬浮收纳盒。

## 技术栈

| 层 | 技术 |
|----|------|
| 运行时 | .NET 10.0 |
| UI 框架 | WPF (Windows Presentation Foundation) |
| 系统托盘 | Hardcodet.NotifyIcon.Wpf |
| 数据绑定 | CommunityToolkit.Mvvm |
| Shell 扩展 | SharpShell (COM via ComHost) |
| 持久化 | JSON (盒状态) + SQLite (日志) |
| 测试 | xUnit + NSubstitute |
| 目标平台 | Windows 10/11 |

## 项目结构

```
simdesk/
├── SimDesk.slnx                  # 解决方案 (.NET 10 新格式)
├── src/
│   ├── SimDesk.Core/             # net10.0 — 纯业务逻辑
│   │   ├── Models/               # 数据模型
│   │   ├── Services/             # 业务服务
│   │   └── Data/                 # 持久化层
│   ├── SimDesk.App/              # net10.0-windows — WPF 应用
│   │   ├── Views/                # WPF 窗口和控件
│   │   ├── Controls/             # 自定义控件
│   │   ├── ViewModels/           # MVVM ViewModels
│   │   ├── Converters/           # 值转换器
│   │   └── Native/               # Win32 P/Invoke
│   └── SimDesk.ShellExtension/   # net10.0-windows — Shell 右键菜单
└── tests/
    └── SimDesk.Core.Tests/       # xUnit 单元测试
```

## 架构分层

```
┌─────────────────────────────────────┐
│ Presentation (WPF)                  │
│ Views, Controls, Converters         │
├─────────────────────────────────────┤
│ Application Logic (ViewModels)     │
│ BoxViewModel, SettingsViewModel     │
├─────────────────────────────────────┤
│ Business Logic (SimDesk.Core)       │
│ BoxManager, FileOperationService    │
│ FileTrackingService, SymlinkService │
│ LogService, PersistenceService      │
├─────────────────────────────────────┤
│ Data Access                         │
│ JsonDataStore, SqliteLogStore       │
└─────────────────────────────────────┘
```

## 关键设计决策

1. **Core 层与 UI 层分离**：SimDesk.Core 不引用任何 WPF/WinForms 程序集，可在 Linux 上完整单元测试
2. **Shell 扩展使用 SharpShell + ComHost**：通过 `Microsoft.Windows.Compatibility` 包在 .NET 10 中运行 .NET Framework 的 SharpShell 库
3. **AllowsTransparency 拖放绕过**：通过 Win32 `DragAcceptFiles` + `WM_DROPFILES` 消息处理
4. **单例模式**：通过全局 Mutex + NamedPipe IPC 保证单实例运行，后续启动转发参数给主实例
