using MH.UI.Controls;
using PictureManager.Common;
using PictureManager.Common.Dialogs;
using PictureManager.Windows.WPF.Views;
using System;
using System.Windows;

namespace PictureManager.Windows.WPF {
  public partial class App {
    public static Core Core => (Core)Current.Properties[nameof(AppProperty.Core)];
    public static AppCore Ui => (AppCore)Current.Properties[nameof(AppProperty.Ui)];
    
    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += delegate (object _, UnhandledExceptionEventArgs args) {
        Dialog.Show(new ErrorDialogM((Exception)args.ExceptionObject));
      };

      MH.UI.WPF.Utils.ColorHelper.AddColorsToResources();

      var splashScreen = new SplashScreenV();
      MainWindow = splashScreen;
      MainWindow.Show();

      await Core.Inst.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.Core)] = Core.Inst;
      Current.Properties[nameof(AppProperty.Ui)] = new AppCore();

      Core.AfterInit();
      Ui.AfterInit();

      ShutdownMode = ShutdownMode.OnMainWindowClose;
      MainWindow = new MainWindowV();
      MainWindow.Show();

      splashScreen.Close();
    }
  }
}
