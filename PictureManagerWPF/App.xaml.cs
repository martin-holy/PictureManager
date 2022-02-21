using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Views;
using System;
using System.Windows;

namespace PictureManager {
  public partial class App {
    public static Core Core => (Core)Current.Properties[nameof(AppProperty.Core)];
    public static AppCore Ui => (AppCore)Current.Properties[nameof(AppProperty.Ui)];
    public static MainWindowV MainWindowV => (MainWindowV)Current.Properties[nameof(AppProperty.MainWindowV)];

    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += delegate (object o, UnhandledExceptionEventArgs exArgs) {
        var wError = new UnhandledErrorDialog { TbError = { Text = ((Exception)exArgs.ExceptionObject).ToString() } };
        wError.ShowDialog();
      };

      var splashScreen = new Views.SplashScreenV();
      MainWindow = splashScreen;
      splashScreen.Show();

      await Core.Instance.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.Core)] = Core.Instance;
      Current.Properties[nameof(AppProperty.Ui)] = new AppCore();
      Current.Properties[nameof(AppProperty.MainWindowV)] = new MainWindowV();

      MainWindow = MainWindowV;
      MainWindowV.Show();

      splashScreen.Close();
    }
  }
}
