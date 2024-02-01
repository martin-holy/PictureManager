using MH.Utils.BaseClasses;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Domain.Dialogs {
  public sealed class AboutDialogM : Dialog {
    private FileVersionInfo _version;

    public FileVersionInfo Version { get => _version; set { _version = value; OnPropertyChanged(); } }
    public string HomePageUrl { get; } = "https://github.com/martin-holy/PictureManager";

    public RelayCommand OpenHomePageCommand { get; }
    
    public AboutDialogM() : base("About", Res.IconImage) {
      Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

      OpenHomePageCommand = new(() => OpenUrl(HomePageUrl));
    }

    private static void OpenUrl(string url) =>
      Process.Start(new ProcessStartInfo(url) {
        UseShellExecute = true
      });
  }
}
