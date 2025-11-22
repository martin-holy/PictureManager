using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.Common.Features.Common;

public sealed class AboutDialog : Dialog {
  public string Version => $"Version: {Core.UiVersion} (Core: {Core.Version})";
  public string? Copyright => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright;
  public string HomePageUrl => "https://github.com/martin-holy/PictureManager";
  public string TelegramUrl => "https://t.me/PictureManager";

  public RelayCommand OpenHomePageCommand { get; }
  public RelayCommand OpenTelegramCommand { get; }
    
  public AboutDialog() : base("About", MH.UI.Res.IconImage) {
    OpenHomePageCommand = new(() => OpenUrl(HomePageUrl));
    OpenTelegramCommand = new(() => OpenUrl(TelegramUrl));
  }

  private static void OpenUrl(string url) =>
    Process.Start(new ProcessStartInfo(url) {
      UseShellExecute = true
    });
}