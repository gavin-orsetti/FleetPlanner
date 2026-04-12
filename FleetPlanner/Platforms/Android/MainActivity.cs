using Android.App;
using Android.Content.PM;

namespace FleetPlanner;

/// <summary>
/// Android's main activity — the entry point that hosts the MAUI app on Android.
/// <para>
/// <b>[Activity] attribute:</b>
/// <list type="bullet">
///   <item><c>Theme = "@style/Maui.SplashTheme"</c> — shows a splash screen while the app loads.</item>
///   <item><c>MainLauncher = true</c> — marks this as the activity launched from the home screen icon.</item>
///   <item><c>ConfigurationChanges = ...</c> — tells Android to NOT restart this activity when
///     screen size, orientation, UI mode, etc. change. MAUI handles these changes internally,
///     so restarting the activity would cause unnecessary app resets.</item>
/// </list>
/// </para>
/// <para>
/// <b>MauiAppCompatActivity:</b> The MAUI-provided base class that bridges Android's Activity
/// lifecycle to MAUI's cross-platform app model. It handles starting the MAUI runtime,
/// setting up the native view hierarchy, and routing Android lifecycle events to MAUI.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/android/"/>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
