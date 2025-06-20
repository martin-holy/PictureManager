using Android.App;
using Android.Content.PM;
using Android.OS;
using PictureManager.Android.Views;
using PictureManager.Common;
using System;
using System.Threading.Tasks;
using Perm = Android.Manifest.Permission;

namespace PictureManager.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity {
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
      CoreUI = new();
      Core.AfterInit(CoreUI);
      CoreUI.AfterInit();

      MH.Utils.Tasks.Dispatch(() => {
        var mainWindow = new MainWindowV(this) { DataContext = Core.VM.MainWindow };
        SetContentView(mainWindow);
      });
    });
  }
}