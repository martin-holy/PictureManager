using MH.UI.WPF.Sample.Views;
using System.Windows;

namespace MH.UI.WPF.Sample;

public partial class App {
  public static Core Core { get; private set; }
  public static CoreUI CoreUI { get; private set; }

  protected override async void OnStartup(StartupEventArgs e) {
    base.OnStartup(e);

    Utils.ColorHelper.AddColorsToResources();

    var splashScreen = new SplashScreenV();
    MainWindow = splashScreen;
    MainWindow.Show();

    await Core.Inst.InitAsync(splashScreen.ProgressMessage);

    Core = Core.Inst;
    CoreUI = new();
    Core.AfterInit();

    ShutdownMode = ShutdownMode.OnMainWindowClose;
    MainWindow = new MainWindowV();
    MainWindow.Show();

    splashScreen.Close();
  }
}