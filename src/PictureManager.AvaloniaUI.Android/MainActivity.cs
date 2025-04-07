using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using PictureManager.AvaloniaUI.Utils;
using Imaging = PictureManager.AvaloniaUI.Android.Utils.Imaging;

namespace PictureManager.AvaloniaUI.Android;

[Activity(
  Label = "PictureManager.AvaloniaUI.Android",
  Theme = "@style/MyTheme.NoActionBar",
  Icon = "@drawable/icon",
  MainLauncher = true,
  ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App> {
  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
    return base.CustomizeAppBuilder(builder)
      .WithInterFont()
      .UseReactiveUI();
  }

  protected override void OnCreate(Bundle? savedInstanceState) {
    RequestPermissions([
      "android.permission.READ_EXTERNAL_STORAGE",
      "android.permission.WRITE_EXTERNAL_STORAGE"], 1);

    ImagingPFactory.Register(() => new Imaging());
    base.OnCreate(savedInstanceState);
  }
}