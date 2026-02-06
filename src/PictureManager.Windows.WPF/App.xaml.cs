using MH.UI.Controls;
using PictureManager.Common;
using PictureManager.Common.Features.Common;
using PictureManager.Windows.WPF.Views;
using PictureManager.Windows.WPF.Views.Layout;
using System;
using System.Reflection;
using System.Windows;

namespace PictureManager.Windows.WPF;

public partial class App {
  public static Core Core { get; private set; } = null!;
  public static CoreUI Ui { get; private set; } = null!;
    
  protected override async void OnStartup(StartupEventArgs e) {
    base.OnStartup(e);

    AppDomain.CurrentDomain.UnhandledException += delegate (object _, UnhandledExceptionEventArgs args) {
      Dialog.ShowAsync(new ErrorDialog((Exception)args.ExceptionObject));
    };

    MH.UI.WPF.Utils.Init.SetDelegates();
    MH.UI.WPF.Utils.ColorHelper.AddColorsToResources();
    Core.UiVersion = Core.GetVersionWithoutHash(Assembly.GetEntryAssembly());

    var splashScreen = new SplashScreenV();
    MainWindow = splashScreen;
    MainWindow.Show();

    await Core.Inst.InitAsync(splashScreen.ProgressMessage, AppDomain.CurrentDomain.BaseDirectory);

    Core = Core.Inst;
    Ui = new();

    Core.AfterInit(Ui);
    Ui.AfterInit();

    ShutdownMode = ShutdownMode.OnMainWindowClose;
    MainWindow = new MainWindowV();
    MainWindow.Show();

    splashScreen.Close();
  }
}