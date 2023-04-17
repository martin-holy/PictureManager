using MH.Utils.BaseClasses;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Domain.Dialogs {
  public sealed class AboutDialogM : Dialog {
    private const string _donateUrl = @"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=9FDUA6VBNWMB2&lc=CZ&item_name=Martin%20Holy&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted";
    private FileVersionInfo _version;

    public FileVersionInfo Version { get => _version; set { _version = value; OnPropertyChanged(); } }
    public string HomePageUrl { get; } = "https://github.com/martin-holy/PictureManager";

    public RelayCommand<object> OpenHomePageCommand { get; }
    public RelayCommand<object> DonanteCommand { get; }
    
    public AboutDialogM() : base("About", Res.IconImage) {
      Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

      OpenHomePageCommand = new(() => OpenUrl(HomePageUrl));
      DonanteCommand = new(() => OpenUrl(_donateUrl));
    }

    private static void OpenUrl(string url) =>
      Process.Start(new ProcessStartInfo(url) {
        UseShellExecute = true
      });
  }
}
