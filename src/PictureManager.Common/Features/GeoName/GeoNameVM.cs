using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.GeoName;

public sealed class GeoNameVM {
  private readonly GeoNameR _r;

  public static AsyncRelayCommand NewGeoNameFromGpsCommand { get; private set; } = null!;

  internal GeoNameVM(GeoNameR r) {
    _r = r;
    NewGeoNameFromGpsCommand = new(_newGeoNameFromGps, Res.IconLocationCheckin, "New GeoName from GPS");
  }

  private async Task _newGeoNameFromGps(CancellationToken token) {
    var inputDialog = new InputDialog(
      "GeoName latitude and longitude",
      "Enter in format: N36.75847,W3.84609",
      Res.IconLocationCheckin,
      string.Empty,
      answer => {
        var (a, b) = GeoNameR.ParseLatLng(answer);
        return a == 0 && b == 0
          ? "Incorrect format"
          : string.Empty;
      });

    if (await Dialog.ShowAsync(inputDialog) != 1) return;

    var (lat, lng) = GeoNameR.ParseLatLng(inputDialog.Answer);
    await _r.CreateGeoNameHierarchy(lat, lng);
  }
}