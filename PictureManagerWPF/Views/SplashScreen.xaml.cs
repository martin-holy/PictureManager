using System;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Views {
  public partial class SplashScreen {
    public IProgress<string> ProgressMessage { get; set; }
    public SplashScreen() {
      InitializeComponent();

      ProgressMessage = new Progress<string>(msg => InfoText.Text = msg);

      VersionInfo.Text = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
    }
  }
}
