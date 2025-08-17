using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Fragment.App;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Layout;
using System;
using System.Threading.Tasks;
using Perm = Android.Manifest.Permission;

namespace PictureManager.Android;

[Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : FragmentActivity {
  public static Core Core { get; private set; } = null!;
  public static CoreUI CoreUI { get; private set; } = null!;

  protected override void OnCreate(Bundle? savedInstanceState) {
    base.OnCreate(savedInstanceState);

    if (CheckSelfPermission(Perm.ReadExternalStorage) != Permission.Granted)
      RequestPermissions([Perm.ReadExternalStorage], 1);

    var splashScreen = new SplashScreenV(this);
    SetContentView(splashScreen);

    Core = Core.Inst;
    Task.Run(async () => {
      await Core.InitAsync(splashScreen.ProgressMessage, AppDomain.CurrentDomain.BaseDirectory);
      CoreUI = new(this);
      Core.AfterInit(CoreUI);
      CoreUI.AfterInit(this);
      RunOnUiThread(() => SetContentView(CoreUI.MainWindow));
    });
  }

  public override void OnBackPressed() {
    if (MainWindowVM.SwitchToBrowserCommand.CanExecute(null)) {
      MainWindowVM.SwitchToBrowserCommand.Execute(null);
      return;
    }
    base.OnBackPressed();
  }

  public override void OnConfigurationChanged(Configuration newConfig) {
    base.OnConfigurationChanged(newConfig);
    // TODO CollectionView ReWrap, ...
  }
}