using System;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App {
    public static AppCore Core => (AppCore) Current.Properties[nameof(AppProperty.AppCore)];
    public static WMain WMain => (WMain) Current.Properties[nameof(AppProperty.WMain)];

    private void App_Startup(object sender, StartupEventArgs e) {
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      Current.Properties[nameof(AppProperty.AppCore)] = new AppCore();
      
      var splashScreen = new Views.SplashScreen();
      MainWindow = splashScreen;
      splashScreen.Show();

      await Core.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.WMain)] = new WMain();
      MainWindow = WMain;
      WMain.Show();
      splashScreen.Close();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
      var wError = new UnhandledErrorDialog { TbError = {Text = ((Exception) e.ExceptionObject).ToString()}};
      wError.ShowDialog();
    }
  }
}
