using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;

namespace PictureManager.Domain.ViewModels;

public sealed class GeoNamesVM {
  private readonly GeoNamesDA _da;

  public static RelayCommand NewGeoNameFromGpsCommand { get; set; }

  public GeoNamesVM(GeoNamesDA da) {
    _da = da;
    NewGeoNameFromGpsCommand = new(NewGeoNameFromGps, Res.IconLocationCheckin, "New GeoName from GPS");
  }

  public async void NewGeoNameFromGps() {
    var inputDialog = new InputDialog(
      "GeoName latitude and longitude",
      "Enter in format: N36.75847,W3.84609",
      Res.IconLocationCheckin,
      string.Empty,
      answer => {
        var (a, b) = GeoNamesDA.ParseLatLng(answer);
        return a == 0 && b == 0
          ? "Incorrect format"
          : string.Empty;
      });

    if (Dialog.Show(inputDialog) != 1) return;

    var (lat, lng) = GeoNamesDA.ParseLatLng(inputDialog.Answer);
    await _da.CreateGeoNameHierarchy(lat, lng);
  }
}