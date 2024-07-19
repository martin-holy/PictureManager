using MH.UI.Controls;
using PictureManager.Common;
using PictureManager.Common.Dialogs;
using PictureManager.Windows.WPF.Views;
using PictureManager.Windows.WPF.Views.Layout;
using System;
using System.Windows;

namespace PictureManager.Windows.WPF;

public partial class App {
  public static Core Core { get; private set; } = null!;
  public static AppCore Ui { get; private set; } = null!;
    
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

    Core = Core.Inst;
    Ui = new();

    Core.AfterInit();
    Ui.AfterInit();

    ShutdownMode = ShutdownMode.OnMainWindowClose;
    MainWindow = new MainWindowV();
    MainWindow.Show();

    splashScreen.Close();
  }
}