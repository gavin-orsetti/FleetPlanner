using Android.Runtime;
using System;

namespace FleetPlanner.Platforms.Android;

public static class AndroidCrashHandler
{
    public static void Register()
    {
        AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
        {
            CrashLogger.WriteLogPublic(args.Exception);
            args.Handled = true;
        };
    }
}
