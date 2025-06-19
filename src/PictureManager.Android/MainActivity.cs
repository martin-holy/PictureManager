using Android.App;
using Android.Content.PM;
using Android.OS;
using PictureManager.Android.Views;
using PictureManager.Common;
using System;
using Perm = Android.Manifest.Permission;

namespace PictureManager.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity {
  public static Core Core { get; private set; } = null!;
  public static CoreUI CoreUI { get; private set; } = null!;

  protected override async void OnCreate(Bundle? savedInstanceState) {
    base.OnCreate(savedInstanceState);

    if (CheckSelfPermission(Perm.ReadExternalStorage) != Permission.Granted)
      RequestPermissions([Perm.ReadExternalStorage], 1);

    var splashScreen = new SplashScreenV(this);
    SetContentView(splashScreen);

    await Core.Inst.InitAsync(splashScreen.ProgressMessage, AppDomain.CurrentDomain.BaseDirectory);
    Core = Core.Inst;
    CoreUI = new();
    Core.AfterInit(CoreUI);
    CoreUI.AfterInit();

    var mainWindow = new MainWindowV(this) { DataContext = Core.VM.MainWindow };
    SetContentView(mainWindow);
  }
}