namespace FleetPlanner.Platforms.Android;

/// <summary>
/// Placeholder for Android-specific crash handling.
/// <para>
/// When implemented, this would register an <c>UncaughtExceptionHandler</c> on the Java thread
/// to catch unhandled exceptions before the Android runtime terminates the process.
/// It's designed to work with the app-level <see cref="FleetPlanner.CrashLogger"/> class
/// to persist crash logs and optionally send them to a reporting service.
/// </para>
/// </summary>
public static class AndroidCrashHandler { }
