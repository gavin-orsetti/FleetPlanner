using ObjCRuntime;
using UIKit;

namespace FleetPlanner;

/// <summary>
/// iOS entry point — the <c>Main()</c> method that bootstraps the iOS application.
/// <para>
/// <b>UIApplication.Main():</b> This is the iOS equivalent of <c>int main()</c> in C.
/// It initializes the UIKit framework and creates the application's run loop.
/// The third parameter (<c>typeof(AppDelegate)</c>) tells UIKit which class to use
/// as the application delegate for handling lifecycle events.
/// </para>
/// <para>
/// This file exists because iOS requires an explicit entry point, unlike Android which
/// uses the <c>[Application]</c> attribute on <c>MainApplication</c>.
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
