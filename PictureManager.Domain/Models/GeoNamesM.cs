using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace PictureManager.Domain.Models;

public sealed class GeoNamesM {
  private readonly GeoNamesDA _da;
  private bool _webLoadDisabledLogged;
  
  public bool ApiLimitExceeded { get; set; }
  public GeoNamesTreeCategory TreeCategory { get; }
  public static RelayCommand NewGeoNameFromGpsCommand { get; set; }

  public GeoNamesM(GeoNamesDA da) {
    _da = da;
    TreeCategory = new(da);
    NewGeoNameFromGpsCommand = new(NewGeoNameFromGps, Res.IconLocationCheckin, "New GeoName from GPS");
  }
  
  public Task<GeoNameM> InsertGeoNameHierarchy(double lat, double lng) =>
    InsertGeoNameHierarchy($"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}".Replace(",", "."));

  public Task<GeoNameM> InsertGeoNameHierarchy(int id) =>
    InsertGeoNameHierarchy($"http://api.geonames.org/hierarchy?geonameId={id}");

  private async Task<GeoNameM> InsertGeoNameHierarchy(string url) {
    if (ApiLimitExceeded) return null;

    if (!Core.Settings.LoadGeoNamesFromWeb) {
      if (!_webLoadDisabledLogged) {
        _webLoadDisabledLogged = true;
        Log.Error("Load GeoNames from web is disabled.", "Enable it in the settings.");
      }

      return null;
    }

    if (string.IsNullOrEmpty(Core.Settings.GeoNamesUserName)) {
      ApiLimitExceeded = true;
      Log.Error("GeoNames user name was not set.", "Please register at geonames.org and set your user name in the settings.");
      return null;
    }

    try {
      var root = await Task.Run(() => {
        var xml = new XmlDocument();
        xml.Load($"{url}&username={Core.Settings.GeoNamesUserName}");
        return xml.SelectSingleNode("/geonames");
      });

      var geoNames = root.SelectNodes("geoname");
      if (geoNames == null) {
        ApiLimitExceeded = true;
        var errorMessage = root.SelectSingleNode("/status")?.Attributes?["message"]?.Value ?? string.Empty;
        Log.Error("Error occurred while retrieving GeoName information.", errorMessage);
        return null;
      }

      GeoNameM gn = null;
      foreach (XmlNode g in geoNames) {
        var geoNameId = int.Parse(g.SelectSingleNode("geonameId")?.InnerText ?? "0");
        gn = _da.All.SingleOrDefault(x => x.GetHashCode() == geoNameId) ??
             _da.ItemCreate(
               geoNameId,
               g.SelectSingleNode("name")?.InnerText,
               g.SelectSingleNode("toponymName")?.InnerText,
               g.SelectSingleNode("fcode")?.InnerText,
               (ITreeItem)gn ?? TreeCategory);
      }

      return gn;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static (double, double) ParseLatLng(string latLng) {
    try {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-"), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-"), CultureInfo.InvariantCulture);

      return (lat, lng);
    }
    catch {
      return (0, 0);
    }
  }

  public async void NewGeoNameFromGps() {
    var inputDialog = new InputDialog(
      "GeoName latitude and longitude",
      "Enter in format: N36.75847,W3.84609",
      Res.IconLocationCheckin,
      string.Empty,
      answer => {
        var (a, b) = ParseLatLng(answer);
        return a == 0 && b == 0
          ? "Incorrect format"
          : string.Empty;
      });

    if (Dialog.Show(inputDialog) != 1) return;

    var (lat, lng) = ParseLatLng(inputDialog.Answer);
    await InsertGeoNameHierarchy(lat, lng);
  }
}