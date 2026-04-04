using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FleetPlanner.WinUI;

/// <summary>
/// Windows (WinUI 3) entry point — hosts the MAUI app on Windows.
/// <para>
/// <b>MauiWinUIApplication:</b> The MAUI-provided base class that bridges WinUI 3's
/// <c>Application</c> lifecycle to MAUI's cross-platform app model. WinUI 3 is the native
/// UI framework for Windows 10/11 apps (successor to UWP XAML).
/// </para>
/// <para>
/// <b>InitializeComponent():</b> Loads the WinUI XAML resources defined in the associated
/// <c>App.xaml</c> file (if any). On Windows, this is separate from the MAUI <c>App.xaml</c>.
/// </para>
/// <para>
/// <b>CreateMauiApp():</b> Delegates to the shared <see cref="MauiProgram.CreateMauiApp"/>
/// composition root — ensuring the same DI container, services, and pages on all platforms.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/windows/"/>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object. This is the first line of authored code
	/// executed, and as such is the logical equivalent of <c>main()</c> or <c>WinMain()</c>.
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

