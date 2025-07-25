using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace PictureManager.MAUI.Droid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
  ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                         ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity {
  protected override void OnCreate(Bundle? savedInstanceState) {
    base.OnCreate(savedInstanceState);

    if (CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) != Permission.Granted)
      RequestPermissions([Android.Manifest.Permission.ReadExternalStorage], 1);
  }
}