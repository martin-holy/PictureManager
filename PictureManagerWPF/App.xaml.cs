using System;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager {
  public partial class App {
    public static AppCore Core => (AppCore) Current.Properties[nameof(AppProperty.AppCore)];
    public static WMain WMain => (WMain) Current.Properties[nameof(AppProperty.WMain)];

    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += delegate(object o, UnhandledExceptionEventArgs exArgs) {
        var wError = new UnhandledErrorDialog {TbError = {Text = ((Exception) exArgs.ExceptionObject).ToString()}};
        wError.ShowDialog();
      };

      var splashScreen = new Views.SplashScreen();
      MainWindow = splashScreen;
      splashScreen.Show();

      Current.Properties[nameof(AppProperty.AppCore)] = new AppCore();
      await Core.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.WMain)] = new WMain();
      MainWindow = WMain;
      WMain.Show();

      splashScreen.Close();
    }
  }
}
