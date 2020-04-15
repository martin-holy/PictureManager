using System;
using System.Threading;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App {
    public static ISplashScreen SplashScreen;
    public static AppCore Core => (AppCore) Current.Properties[nameof(AppProperty.AppCore)];
    public static WMain WMain => (WMain) Current.Properties[nameof(AppProperty.WMain)];

    private ManualResetEvent _resetSplashCreated;
    private Thread _splashThread;

    private void App_Startup(object sender, StartupEventArgs e) {
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      var argument = "";
      if (e.Args.Length != 0) argument = e.Args[0];

      Current.Properties[nameof(AppProperty.AppCore)] = new AppCore();
      Current.Properties[nameof(AppProperty.WMain)] = new WMain(argument);

      WMain.Show();
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

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
      var wError = new UnhandledErrorDialog { TbError = {Text = ((Exception) e.ExceptionObject).ToString()}};
      wError.ShowDialog();
    }
  }
}
