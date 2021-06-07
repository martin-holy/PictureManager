using System;
using System.Windows;
using PictureManager.Dialogs;
using PictureManager.Domain;

namespace PictureManager {
  public partial class App {
    public static Core Core => (Core) Current.Properties[nameof(AppProperty.Core)];
    public static SimpleDB.SimpleDB Db => (SimpleDB.SimpleDB) Current.Properties[nameof(AppProperty.Db)];
    public static AppCore Ui => (AppCore)Current.Properties[nameof(AppProperty.Ui)];
    public static WMain WMain => (WMain)Current.Properties[nameof(AppProperty.WMain)];

    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += delegate(object o, UnhandledExceptionEventArgs exArgs) {
        var wError = new UnhandledErrorDialog {TbError = {Text = ((Exception) exArgs.ExceptionObject).ToString()}};
        wError.ShowDialog();
      };

      var splashScreen = new Views.SplashScreen();
      MainWindow = splashScreen;
      splashScreen.Show();

      await Core.Instance.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.Core)] = Core.Instance;
      Current.Properties[nameof(AppProperty.Db)] = Core.Instance.Sdb;
      Current.Properties[nameof(AppProperty.Ui)] = new AppCore();
      Current.Properties[nameof(AppProperty.WMain)] = new WMain();

      MainWindow = WMain;
      WMain.Show();

      splashScreen.Close();
    }
  }
}
