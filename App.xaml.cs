using System.Windows;
using Serilog;

namespace OpenClaw.DesignHub;

/// <summary>
/// 应用入口
/// </summary>
public partial class App : Application
{
    public static ILogger Logger { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 初始化日志
        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenClaw.DesignHub",
            "Logs",
            "app-.log"
        );

        Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Logger.Information("=== OpenClaw DesignHub 启动 ===");

        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Logger.Fatal(ex, "未处理的异常");
            Log.CloseAndFlush();
            Environment.Exit(1);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Logger.Error(args.Exception, "UI 线程异常");
            args.Handled = true;
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Information("=== OpenClaw DesignHub 退出 ===");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}