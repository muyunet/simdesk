using WixSharp;
using WixSharp.CommonTasks;

// ============================================================
// SimDesk MSI 安装包生成器（WixSharp）
//
// 用法:  dotnet run --project src/SimDesk.Installer
// 前提:  先 dotnet publish 产出 publish/SimDesk 和 publish/ShellExt
// ============================================================

const string AppName = "SimDesk";
const string Manufacturer = "SimDesk";
const string Version = "0.1.0";
var productGuid = Guid.NewGuid();

var repoRoot = Path.GetFullPath(
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
var publishDir = Path.Combine(repoRoot, "publish");
var appDir = Path.Combine(publishDir, "SimDesk");
var shellDir = Path.Combine(publishDir, "ShellExt");

if (!Directory.Exists(appDir))
{
    Console.Error.WriteLine($"[错误] 找不到 App 发布目录: {appDir}");
    Console.Error.WriteLine("请先: dotnet publish src/SimDesk.App -c Release -r win-x64 --self-contained -o publish/SimDesk");
    Environment.Exit(1);
}

Console.WriteLine($"[信息] App  : {appDir}");
Console.WriteLine($"[信息] Shell: {shellDir}");

// ============================================================
// 安装包结构
// ============================================================

var project = new Project(AppName,

    // %ProgramFiles%\SimDesk
    new Dir(new Id("INSTALLDIR"), @"%ProgramFiles%\SimDesk",
        new Files(Path.Combine(appDir, "*.*")),
        new Dir("ShellExt",
            Directory.Exists(shellDir)
                ? new Files(Path.Combine(shellDir, "*.*"))
                : []
        )
    ),

    // 桌面快捷方式
    new Dir(@"%Desktop%",
        new ExeFileShortcut(AppName, "[INSTALLDIR]SimDesk.App.exe", "")
        {
            WorkingDirectory = "[INSTALLDIR]"
        }
    ),

    // 开始菜单
    new Dir(@"%ProgramMenu%\SimDesk",
        new ExeFileShortcut(AppName, "[INSTALLDIR]SimDesk.App.exe", "")
        {
            WorkingDirectory = "[INSTALLDIR]"
        }
    )
);

project.GUID = productGuid;
project.Version = new Version(Version);
project.MajorUpgradeStrategy = new MajorUpgradeStrategy
{
    UpgradeVersions = VersionRange.OlderThanThis,
    PreventDowngradingVersions = VersionRange.NewerThanThis,
    NewerProductInstalledErrorMessage = "已安装更新版本。"
};

project.ControlPanelInfo.Manufacturer = Manufacturer;
project.ControlPanelInfo.Comments = "Windows 桌面整理工具";
project.ControlPanelInfo.HelpLink = "https://github.com/muyunet/simdesk";

// ============================================================
// 自定义操作：注册 / 注销 Shell COM 扩展
// ============================================================

project.Actions = new WixSharp.Action[]
{
    new ElevatedManagedAction(
        "RegisterComHost",
        typeof(ComActions).GetMethod(nameof(ComActions.Register))!,
        Return.check,
        When.After,
        Step.InstallFiles,
        Condition.NOT_Installed
    ),
    new ElevatedManagedAction(
        "UnregisterComHost",
        typeof(ComActions).GetMethod(nameof(ComActions.Unregister))!,
        Return.check,
        When.Before,
        Step.RemoveFiles,
        Condition.BeingUninstalled
    )
};

// ============================================================
// 构建
// ============================================================

var msiPath = Path.Combine(publishDir, $"{AppName}-{Version}-x64.msi");
Console.WriteLine($"[构建] 正在生成 {msiPath} ...");

project.BuildMsi(msiPath);

Console.WriteLine($"[完成] {msiPath}");

// ============================================================
// COM 注册操作（MSI 自定义操作在安装/卸载时调用）
// ============================================================

public class ComActions
{
    [CustomAction]
    public static ActionResult Register(Session session)
    {
        var comHost = Path.Combine(session["INSTALLDIR"], "ShellExt",
            "SimDesk.ShellExtension.comhost.dll");

        if (!File.Exists(comHost))
        {
            session.Log($"COM host 不存在: {comHost}");
            return ActionResult.NotExecuted;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "regsvr32",
                Arguments = $"/s \"{comHost}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = System.Diagnostics.Process.Start(psi)!;
            p.WaitForExit();
            session.Log($"regsvr32 退出码: {p.ExitCode}");
            return p.ExitCode == 0 ? ActionResult.Success : ActionResult.Failure;
        }
        catch (Exception ex)
        {
            session.Log($"注册失败: {ex.Message}");
            return ActionResult.Failure;
        }
    }

    [CustomAction]
    public static ActionResult Unregister(Session session)
    {
        var comHost = Path.Combine(session["INSTALLDIR"], "ShellExt",
            "SimDesk.ShellExtension.comhost.dll");

        if (!File.Exists(comHost))
            return ActionResult.NotExecuted;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "regsvr32",
                Arguments = $"/s /u \"{comHost}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = System.Diagnostics.Process.Start(psi)!;
            p.WaitForExit();
            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"注销失败: {ex.Message}");
            return ActionResult.Failure;
        }
    }
}
