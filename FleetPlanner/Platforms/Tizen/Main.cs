using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace FleetPlanner;

/// <summary>
/// Tizen entry point — hosts the MAUI app on Samsung Tizen OS (smart TVs, watches, IoT).
/// <para>
/// <b>MauiApplication (Tizen):</b> On Tizen, the program class itself is the application.
/// It extends <c>MauiApplication</c> and provides a <c>Main()</c> entry point that creates
/// the program instance and calls <c>Run()</c> to start the Tizen application event loop.
/// </para>
/// <para>
/// This is the least commonly targeted platform for MAUI apps, but MAUI includes it for
/// completeness. The app may not render optimally on Tizen without platform-specific
/// adjustments, but the same shared code runs unchanged.
/// </para>
/// </summary>
class Program : MauiApplication
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	static void Main(string[] args)
	{
		var app = new Program();
		app.Run(args);
	}
}
