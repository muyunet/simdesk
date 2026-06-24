# SimDesk Windows 部署指南

## 前提条件

目标 Windows 机器需要：
- Windows 10 1809+ 或 Windows 11
- (可选) .NET 10 Runtime — 如果用自包含发布则不需要

## 1. 编译发布

### 在 Linux 上交叉编译

```bash
# 发布为 Windows x64 自包含单文件
dotnet publish src/SimDesk.App/SimDesk.App.csproj \
    --runtime win-x64 \
    --configuration Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o publish/SimDesk

# 发布 Shell Extension
dotnet publish src/SimDesk.ShellExtension/SimDesk.ShellExtension.csproj \
    --runtime win-x64 \
    --configuration Release \
    --self-contained true \
    -o publish/ShellExt
```

### 在 Windows 上编译（推荐）

```powershell
dotnet publish src/SimDesk.App/SimDesk.App.csproj `
    -c Release --self-contained true `
    -p:PublishSingleFile=true `
    -o publish/SimDesk

dotnet publish src/SimDesk.ShellExtension/SimDesk.ShellExtension.csproj `
    -c Release --self-contained true `
    -o publish/ShellExt
```

## 2. 安装步骤

### 2.1 复制文件

将发布输出复制到目标安装目录，例如：
```
C:\Program Files\SimDesk\
├── SimDesk.App.exe
├── SimDesk.Core.dll
├── ... (依赖 DLL)
└── ShellExt\
    ├── SimDesk.ShellExtension.comhost.dll
    └── ... (Shell 扩展 DLL)
```

### 2.2 注册 Shell 扩展

以**管理员身份**打开 PowerShell：

```powershell
cd "C:\Program Files\SimDesk\ShellExt"
regsvr32 SimDesk.ShellExtension.comhost.dll

# 重启资源管理器以加载右键菜单
taskkill /f /im explorer.exe
start explorer.exe
```

### 2.3 设置开机自启

运行 SimDesk.App.exe，右键托盘图标 → 勾选"开机自启"

或者手动添加到注册表：
```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
  SimDesk = "C:\Program Files\SimDesk\SimDesk.App.exe" --minimized
```

## 3. 卸载

### 3.1 取消开机自启

托盘右键 → 取消"开机自启"勾选

### 3.2 注销 Shell 扩展

管理员 PowerShell：
```powershell
regsvr32 /u "C:\Program Files\SimDesk\ShellExt\SimDesk.ShellExtension.comhost.dll"
taskkill /f /im explorer.exe
start explorer.exe
```

### 3.3 删除程序文件

删除 `C:\Program Files\SimDesk\` 目录

### 3.4 清理数据（可选）

删除 `%APPDATA%\SimDesk\` 目录（包含所有收纳盒文件和日志）

## 4. 故障排除

| 问题 | 解决 |
|------|------|
| 右键菜单不出现 | 以管理员重新运行 regsvr32 注册，重启 explorer |
| 运行时报缺少 .NET | 安装 .NET 10 Runtime 或用 --self-contained 发布 |
| 符号链接创建失败 | Win10 需开启"开发者模式"（设置→更新→开发者选项） |
| 拖放文件无效 | 检查 AllowsTransparency 设置，确认 WM_DROPFILES 已注册 |
