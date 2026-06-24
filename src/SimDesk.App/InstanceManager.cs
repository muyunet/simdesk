using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace SimDesk.App;

/// <summary>
/// 单例管理 + 进程间 NamedPipe 通信
/// </summary>
public class InstanceManager : IDisposable
{
    private const string AppMutexName = @"Global\SimDesk-SingleInstance";
    private const string PipeName = "SimDesk-Pipe";

    private Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cts;
    private bool _isMainInstance;

    /// <summary>
    /// 尝试成为主实例。如果已有实例在运行，则转发参数并返回 false
    /// </summary>
    public bool TryBecomeMainInstance(string[] args)
    {
        _mutex = new Mutex(initiallyOwned: true, AppMutexName, out bool createdNew);

        if (createdNew)
        {
            _isMainInstance = true;
            StartPipeServer();
            return true;
        }

        // 已有实例运行，转发参数
        ForwardArgsToMain(args);
        return false;
    }

    private void StartPipeServer()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await _pipeServer.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(_pipeServer, Encoding.UTF8);
                    var argsLine = await reader.ReadLineAsync(token);

                    if (argsLine != null)
                    {
                        var args = argsLine.Split('\0');
                        // 在主 UI 线程处理参数
                        Application.Current?.Dispatcher.Invoke(() =>
                            HandleForwardedArgs(args));
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (IOException) { /* 连接中断，继续监听 */ }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }
        }, token);
    }

    private void ForwardArgsToMain(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 2000);

            using var writer = new StreamWriter(client, Encoding.UTF8);
            writer.WriteLine(string.Join('\0', args));
            writer.Flush();
        }
        catch (TimeoutException)
        {
            // 无法连接主实例，忽略
        }
    }

    private void HandleForwardedArgs(string[] args)
    {
        // TODO: 解析命令行参数并执行相应操作
        // 可能的 action: create-empty-box, create-box-with-files, create-box-from-folder
    }

    public bool IsMainInstance => _isMainInstance;

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _pipeServer?.Dispose();
        _mutex?.Close();
    }
}
