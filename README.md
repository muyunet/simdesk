# SimDesk

Windows 桌面整理工具 — 用收纳盒让你的桌面井井有条。

## 特性

- **无主窗口** — 仅系统托盘图标，不打扰你的工作流
- **悬浮收纳盒** — 直接放在桌面上的可拖动窗口，支持拖放文件
- **两种收纳模式**
  - **常规收纳盒**：文件真实存储其中，适合归档整理
  - **链接收纳盒**：盒内仅存符号链接，修改即修改原文件，适合快捷访问
- **灵活的设置** — 每个收纳盒独立设置：图标/列表视图、排序方式、背景色/图片、透明度
- **解散还原** — 解散收纳盒时文件自动回到原始位置，冲突文件自动重命名
- **完整日志** — 所有操作可追溯，解散操作可逐文件查看还原位置并定位
- **右键集成** — 桌面/文件/文件夹右键直接创建收纳盒

## 系统要求

- Windows 10 1809+ 或 Windows 11
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)（或使用自包含发布）

## 快速开始

### 编译

```bash
# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行测试（Linux 上也可运行）
dotnet test

# 发布为 Windows 自包含单文件
dotnet publish src/SimDesk.App -c Release --self-contained true -p:PublishSingleFile=true -o publish
```

### 安装 Shell 扩展

以管理员身份运行 PowerShell：

```powershell
cd publish
regsvr32 SimDesk.ShellExtension.comhost.dll
taskkill /f /im explorer.exe && start explorer.exe
```

## 项目结构

```
simdesk/
├── src/
│   ├── SimDesk.Core/             # 业务逻辑层（纯 C#，Linux 可测试）
│   │   ├── Models/               # 数据模型
│   │   ├── Services/             # 核心服务
│   │   └── Data/                 # 持久化层 (JSON + SQLite)
│   ├── SimDesk.App/              # WPF 应用
│   │   ├── Views/                # 窗口和控件
│   │   ├── ViewModels/           # MVVM ViewModels
│   │   └── Native/               # Win32 P/Invoke
│   └── SimDesk.ShellExtension/   # SharpShell COM 右键菜单
├── tests/
│   └── SimDesk.Core.Tests/       # xUnit 单元测试
└── docs/
    ├── architecture.md           # 架构文档
    ├── ui-spec.md                # UI 行为验证清单
    └── publish-guide.md          # 部署指南
```

## 技术栈

| 层 | 技术 |
|----|------|
| 运行时 | .NET 10 |
| UI 框架 | WPF |
| 系统托盘 | Hardcodet.NotifyIcon.Wpf |
| Shell 扩展 | SharpShell (COM) |
| 持久化 | JSON + SQLite |
| 测试 | xUnit |

## 开发

本项目在 Linux 上开发，通过 `.NET 10 SDK` 交叉编译为 Windows 目标。`SimDesk.Core` 层完全与 UI 解耦，可在 Linux 上完整运行单元测试。

```bash
# 运行测试
dotnet test tests/SimDesk.Core.Tests/
```

## 许可证

本项目使用 GNU General Public License v3.0 — 详见 [LICENSE](LICENSE)。
