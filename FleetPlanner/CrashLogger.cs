using System;
using System.IO;
using System.Threading.Tasks;

namespace FleetPlanner;

public static class CrashLogger
{
    private static string LogPath =>
        Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public static void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            WriteLogPublic(ex);
            ShowAlert(ex).GetAwaiter().GetResult();
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            WriteLogPublic(args.Exception);
            args.SetObserved();
            ShowAlert(args.Exception).GetAwaiter().GetResult();
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

    private static async Task ShowAlert(Exception? ex)
    {
        try
        {
            var stackTrace = ex?.StackTrace ?? string.Empty;
            var truncated = stackTrace.Substring(0, Math.Min(stackTrace.Length, 800));
            var message = $"Type: {ex?.GetType().Name}\n\nMessage: {ex?.Message}\n\nInner: {ex?.InnerException?.Message}\n\nStack:\n{truncated}";
            if (Application.Current?.MainPage is not null)
            {
                await Application.Current.MainPage.DisplayAlert("Crash Details", message, "OK");
            }
        }
        catch { }
    }
}
