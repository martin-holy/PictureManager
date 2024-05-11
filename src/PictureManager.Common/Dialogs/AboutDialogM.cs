using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Common.Dialogs {
  public sealed class AboutDialogM : Dialog {
    private FileVersionInfo _version;

    public FileVersionInfo Version { get => _version; set { _version = value; OnPropertyChanged(); } }
    public string HomePageUrl => "https://github.com/martin-holy/PictureManager";
    public string TelegramUrl => "https://t.me/PictureManager";

    public RelayCommand OpenHomePageCommand { get; }
    public RelayCommand OpenTelegramCommand { get; }
    
    public AboutDialogM() : base("About", MH.UI.Res.IconImage) {
      Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

      OpenHomePageCommand = new(() => OpenUrl(HomePageUrl));
      OpenTelegramCommand = new(() => OpenUrl(TelegramUrl));
    }

    private static void OpenUrl(string url) =>
      Process.Start(new ProcessStartInfo(url) {
        UseShellExecute = true
      });
  }
}
