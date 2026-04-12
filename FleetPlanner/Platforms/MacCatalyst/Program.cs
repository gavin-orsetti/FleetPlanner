using ObjCRuntime;
using UIKit;

namespace FleetPlanner;

/// <summary>
/// Mac Catalyst entry point — identical to the iOS <c>Program.cs</c>.
/// <para>
/// Mac Catalyst uses UIKit (not AppKit), so the bootstrap process is the same as iOS:
/// call <c>UIApplication.Main()</c> to initialize the application run loop and delegate.
/// </para>
/// </summary>
public class Program
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
