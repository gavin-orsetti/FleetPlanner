using Android.App;
using Android.Content.PM;
using Android.OS;
using FleetPlanner.Platforms.Android;

namespace FleetPlanner;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        AndroidCrashHandler.Register();
        base.OnCreate(savedInstanceState);
    }
}
