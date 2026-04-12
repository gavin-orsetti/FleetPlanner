using Android.App;
using Android.Runtime;

namespace FleetPlanner;

/// <summary>
/// Android Application class — the process-level entry point for the Android app.
/// <para>
/// <b>[Application] attribute:</b> Marks this class as the Android Application subclass.
/// Android creates this before any Activity, Service, or BroadcastReceiver. It lives for
/// the entire process lifetime — longer than any single Activity.
/// </para>
/// <para>
/// <b>Constructor:</b> The <c>(IntPtr, JniHandleOwnership)</c> constructor is required by
/// the Xamarin/MAUI Android binding layer. It's called when the managed object wraps an
/// existing Java object (the Android runtime creates the Application instance in Java first,
/// then this constructor bridges it to the .NET side).
/// </para>
/// <para>
/// <b>CreateMauiApp():</b> Delegates to <see cref="MauiProgram.CreateMauiApp"/> — the
/// shared composition root where all DI services, ViewModels, and pages are registered.
/// Every platform entry point calls this same method, ensuring identical app configuration
/// across Android, iOS, Mac, Windows, and Tizen.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/android/"/>
[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
