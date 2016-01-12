using System;
using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App {
    void App_Startup(object sender, StartupEventArgs e) {
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      string argument = "";
      if (e.Args.Length != 0) argument = e.Args[0];
      WMain wMain = new WMain(argument);
      wMain.Show();
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
      WError wError = new WError {TbError = {Text = ((Exception) e.ExceptionObject).ToString()}};
      wError.ShowDialog();
    }
  }
}
