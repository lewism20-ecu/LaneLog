using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;

namespace LaneLog.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		UseLightStatusBarIcons();
	}

	public override void OnConfigurationChanged(Configuration newConfig)
	{
		base.OnConfigurationChanged(newConfig);
		UseLightStatusBarIcons();
	}

	private void UseLightStatusBarIcons()
	{
		if (Window is not { DecorView: { } decorView } window)
		{
			Android.Util.Log.Warn("LaneLog", "Could not access the window to configure status bar icons.");
			return;
		}

		var controller = WindowCompat.GetInsetsController(window, decorView);
		if (controller is null)
		{
			Android.Util.Log.Warn("LaneLog", "Could not configure status bar icons.");
			return;
		}

		controller.AppearanceLightStatusBars = false;
	}
}
