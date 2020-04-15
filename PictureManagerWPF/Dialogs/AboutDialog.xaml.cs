using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace PictureManager.Dialogs {
  public partial class AboutDialog {
    public AboutDialog() {
      InitializeComponent();
      DataContext = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
    }

    private void BtnDonate_OnClick(object sender, RoutedEventArgs e) {
      Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=9FDUA6VBNWMB2&lc=CZ&item_name=Martin%20Holy&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donate_LG%2egif%3aNonHosted");
    }

    private void Homepage_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
      Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }
  }
}
