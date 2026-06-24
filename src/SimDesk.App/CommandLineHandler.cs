namespace SimDesk.App;

/// <summary>
/// 命令行参数解析和分发
/// 从 Shell 右键菜单启动时，通过命令行参数指明操作
/// </summary>
public class CommandLineHandler
{
    public enum AppAction
    {
        None,
        CreateEmptyBox,
        CreateBoxWithFiles,
        CreateBoxFromFolder
    }

    public record CommandLineArgs(
        AppAction Action,
        string? Path,      // %V — 目标目录
        string? Files      // 选中的文件路径（多个用 | 分隔）
    );

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    public static CommandLineArgs Parse(string[] args)
    {
        var action = AppAction.None;
        string? path = null;
        string? files = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--action":
                    if (i + 1 < args.Length)
                    {
                        action = args[++i] switch
                        {
                            "create-empty-box" => AppAction.CreateEmptyBox,
                            "create-box-with-files" => AppAction.CreateBoxWithFiles,
                            "create-box-from-folder" => AppAction.CreateBoxFromFolder,
                            _ => AppAction.None
                        };
                    }
                    break;

                case "--path":
                    if (i + 1 < args.Length)
                        path = args[++i];
                    break;

                case "--files":
                    if (i + 1 < args.Length)
                        files = args[++i];
                    break;

                case "--minimized":
                    // 开机自启时使用，不显示任何窗口
                    break;
            }
        }

        return new CommandLineArgs(action, path, files);
    }
}
