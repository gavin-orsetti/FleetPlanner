using Foundation;

namespace FleetPlanner;

/// <summary>
/// iOS Application Delegate — the entry point that hosts the MAUI app on iOS.
/// <para>
/// <b>[Register("AppDelegate")]:</b> An Objective-C interop attribute that registers this class
/// with the iOS runtime under the name "AppDelegate". iOS requires an application delegate
/// that responds to lifecycle events (launch, background, terminate).
/// </para>
/// <para>
/// <b>MauiUIApplicationDelegate:</b> The MAUI-provided base class that bridges iOS's
/// <c>UIApplicationDelegate</c> lifecycle to MAUI's cross-platform app model.
/// </para>
/// <para>
/// <b>CreateMauiApp():</b> Delegates to the shared <see cref="MauiProgram.CreateMauiApp"/>
/// composition root — the same method called by all platform entry points.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/ios/"/>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
