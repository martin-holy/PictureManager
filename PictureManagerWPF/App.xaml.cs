using System;
using System.Threading;
using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App {
    public static ISplashScreen SplashScreen;
    private ManualResetEvent _resetSplashCreated;
    private Thread _splashThread;

    void App_Startup(object sender, StartupEventArgs e) {
      #if !DEBUG
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      #endif
      string argument = "";
      if (e.Args.Length != 0) argument = e.Args[0];
      WMain wMain = new WMain(argument);
      wMain.Show();
    }

    protected override void OnStartup(StartupEventArgs e) {
      _splashThread = new Thread(ShowSplash);
      _splashThread.SetApartmentState(ApartmentState.STA);
      _splashThread.IsBackground = true;
      _splashThread.Name = "Splash Screen";
      _splashThread.Start();

      _resetSplashCreated = new ManualResetEvent(false);
      _resetSplashCreated.WaitOne();
      base.OnStartup(e);
    }

    private void ShowSplash() {
      var splashScreen = new WSplashScreen();
      SplashScreen = splashScreen;
      splashScreen.Show();
      _resetSplashCreated.Set();
      System.Windows.Threading.Dispatcher.Run();
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
      WError wError = new WError {TbError = {Text = ((Exception) e.ExceptionObject).ToString()}};
      wError.ShowDialog();
    }
  }
}
