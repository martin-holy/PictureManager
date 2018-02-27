using System.Diagnostics;
using System.Reflection;

namespace PictureManager {
  public interface ISplashScreen {
    void AddMessage(string message);
    void LoadComplete();
  }

  /// <summary>
  /// Interaction logic for WSplashScreen.xaml
  /// </summary>
  public partial class WSplashScreen : ISplashScreen {
    public WSplashScreen() {
      InitializeComponent();

      VersionInfo.Text = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
    }

    public void AddMessage(string message) {
      Dispatcher.Invoke(delegate { InfoText.Text = message; });
    }

    public void LoadComplete() {
      Dispatcher.InvokeShutdown();
    }
  }
}
