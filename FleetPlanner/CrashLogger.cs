using System;
using System.IO;
using System.Threading.Tasks;

namespace FleetPlanner;

public static class CrashLogger
{
    // FIX: Cache the log path lazily. FileSystem.AppDataDirectory is not
    // available until the MAUI platform is initialised, but CrashLogger.Register()
    // is called very early in MauiProgram.CreateMauiApp(). Deferring the path
    // resolution to first write prevents an early-access crash on Android.
    private static string? _logPath;
    private static string LogPath => _logPath ??= Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public static void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            WriteLogPublic(ex);
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            WriteLogPublic(args.Exception);
            args.SetObserved();
        };
    }

    public static void WriteLogPublic(Exception? ex)
    {
        try
        {
            var content = $"[{DateTime.Now:O}]\n{ex?.GetType().FullName}\n{ex?.Message}\n{ex?.StackTrace}\n\nInner: {ex?.InnerException?.Message}\n{ex?.InnerException?.StackTrace}\n\n";
            File.AppendAllText(LogPath, content);
        }
        catch { /* swallow - logging must never crash the crash handler */ }
    }
}
