using PictureManager.Dialogs;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public static class GeoNamesBaseVM {
    public static bool IsGeoNamesUserNameInSettings() {
      if (!string.IsNullOrEmpty(Settings.Default.GeoNamesUserName)) return true;

      MessageDialog.Show(
        "GeoNames User Name",
        "GeoNames user name was not found.\nPlease register at geonames.org and set your user name in the settings.",
        false);

      return false;
    }
  }
}
