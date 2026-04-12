using Foundation;

namespace FleetPlanner;

/// <summary>
/// Mac Catalyst Application Delegate — the entry point for the MAUI app on macOS.
/// <para>
/// <b>Mac Catalyst:</b> This is Apple's technology for running iPad/iOS apps on macOS.
/// The app delegate is identical to the iOS version — same base class, same registration.
/// Mac Catalyst uses UIKit (not AppKit), so the MAUI app runs as if it were an iPad app
/// with automatic macOS UI adaptations (menu bar, window resizing, etc.).
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/mac-catalyst/"/>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
