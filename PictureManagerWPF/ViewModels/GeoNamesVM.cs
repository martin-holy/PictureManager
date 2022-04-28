using MH.UI.WPF.BaseClasses;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public static class GeoNamesVM {
    public static RelayCommand<object> NewGeoNameFromGpsCommand { get; } = new(
      () => App.Core.GeoNamesM.NewGeoNameFromGps(Settings.Default.GeoNamesUserName));
  }
}
