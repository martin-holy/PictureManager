using PictureManager.Common;
using PictureManager.MAUI.Views;
using PictureManager.MAUI.Views.Layout;
using System;
using System.Threading.Tasks;

namespace PictureManager.MAUI;

public partial class App {
  public static Core Core { get; private set; } = null!;
  public static CoreUI CoreUI { get; private set; } = null!;

  public App() {
    InitializeComponent();

    MH.UI.MAUI.Utils.ColorHelper.AddColorsToResources(Resources);

    // TODO PORT
    var splashScreen = new SplashScreenV();
    var mainPage = new MainPage();
    MainPage = mainPage;

    Core.Inst.InitAsync(splashScreen.ProgressMessage, AppDomain.CurrentDomain.BaseDirectory).ContinueWith(_ => {
      Core = Core.Inst;
      CoreUI = new();
      Core.AfterInit(CoreUI);
      CoreUI.AfterInit();
      mainPage.BindingContext = Core.VM.MainWindow;
      //MainPage = new MainPage(); // changing MainPage doesn't work well
    });
  }
}