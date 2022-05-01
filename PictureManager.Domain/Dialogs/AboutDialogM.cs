using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Domain.Dialogs {
  public sealed class AboutDialogM : ObservableObject, IDialog {
    private const string _donateUrl = @"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=9FDUA6VBNWMB2&lc=CZ&item_name=Martin%20Holy&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted";
    private FileVersionInfo _version;
    private string _title;
    private int _result = -1;

    public FileVersionInfo Version { get => _version; set { _version = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public string HomePageUrl { get; } = "https://github.com/martin-holy/PictureManager";

    public RelayCommand<object> OpenHomePageCommand { get; }
    public RelayCommand<object> DonanteCommand { get; }
    
    public AboutDialogM() {
      Title = "About";
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
