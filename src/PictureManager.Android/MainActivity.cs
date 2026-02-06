using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using PictureManager.Android.Views;
using PictureManager.Common;
using PictureManager.Common.Layout;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Perm = Android.Manifest.Permission;

namespace PictureManager.Android;

[Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : FragmentActivity {
  private Core _core = null!;
  private CoreUI _coreUI = null!;
  private bool _disposed;

  protected override void OnCreate(Bundle? savedInstanceState) {
    base.OnCreate(savedInstanceState);

    global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => {
      MH.Utils.Log.Error(e.Exception, "Unhandled Exception");
      e.Handled = true;
    };

    var permissions = new string[] {
      Perm.ReadExternalStorage,
      Perm.WriteExternalStorage
    };

    if (permissions.Any(p => ContextCompat.CheckSelfPermission(this, p) != Permission.Granted))
      ActivityCompat.RequestPermissions(this, permissions, 0);

    MH.UI.Android.Utils.Init.SetDelegates();
    Core.UiVersion = PackageManager!.GetPackageInfo(PackageName!, 0)?.VersionName ?? "?";

    var splashScreen = new SplashScreenV(this);
    SetContentView(splashScreen);

    _core = Core.Inst;
    Task.Run(async () => {
      var appDir = GetExternalFilesDir(null)?.AbsolutePath
        ?? FilesDir?.AbsolutePath
        ?? throw new InvalidOperationException("Unable to resolve app storage directory.");
      
      await _core.InitAsync(splashScreen.ProgressMessage, appDir);
      _coreUI = new(this);
      _core.AfterInit(_coreUI);
      _coreUI.AfterInit(this);
      RunOnUiThread(() => SetContentView(_coreUI.MainWindow));
    });
  }

  public override void OnBackPressed() {
    if (MainWindowVM.SwitchToBrowserCommand.CanExecute(null)) {
      MainWindowVM.SwitchToBrowserCommand.Execute(null);
      return;
    }
    base.OnBackPressed();
  }

  protected override void OnStop() {
    Core.R?.BackUp();
    base.OnStop();
  }

  public override void OnConfigurationChanged(Configuration newConfig) {
    base.OnConfigurationChanged(newConfig);
    // TODO CollectionView ReWrap, ...
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _coreUI.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }
}