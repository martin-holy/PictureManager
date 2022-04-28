using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using MH.Utils.Dialogs;
using PictureManager.Domain.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class GeoNamesM : TreeCategoryBase {
    public DataAdapter DataAdapter { get; set; }
    public List<GeoNameM> All { get; } = new();
    public Dictionary<int, GeoNameM> AllDic { get; set; }

    public GeoNamesM() : base(Res.IconLocationCheckin, Category.GeoNames, "GeoNames") { }

    public GeoNameM InsertGeoNameHierarchy(double lat, double lng, string userName) {
      try {
        var url = $"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}&username={userName}".Replace(",", ".");
        var xml = new XmlDocument();
        xml.Load(url);
        var geonames = xml.SelectNodes("/geonames/geoname");
        if (geonames == null) return null;

        GeoNameM parentGeoName = null;
        foreach (XmlNode geoname in geonames) {
          var geoNameId = int.Parse(geoname.SelectSingleNode("geonameId")?.InnerText ?? "0");
          var dbGeoName = All.SingleOrDefault(x => x.Id == geoNameId);

          if (dbGeoName == null) {
            dbGeoName = new(
              geoNameId,
              geoname.SelectSingleNode("name")?.InnerText,
              geoname.SelectSingleNode("toponymName")?.InnerText,
              geoname.SelectSingleNode("fcode")?.InnerText,
              parentGeoName);

            All.Add(dbGeoName);
            parentGeoName?.Items.Add(dbGeoName);
            Core.RunOnUiThread(() => DataAdapter.IsModified = true);
          }

          parentGeoName = dbGeoName;
        }

        return parentGeoName;
      }
      catch (Exception ex) {
        Core.Instance.LogError(ex);
        return null;
      }
    }

    private (double, double) ParseLatLng(string latLng) {
      try {
        var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-"), CultureInfo.InvariantCulture);
        var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-"), CultureInfo.InvariantCulture);

        return (lat, lng);
      }
      catch {
        return (0, 0);
      }
    }

    public static bool IsGeoNamesUserNameInSettings(string userName) {
      if (!string.IsNullOrEmpty(userName)) return true;

      Core.DialogHostShow(new MessageDialog(
        "GeoNames User Name",
        "GeoNames user name was not found.\nPlease register at geonames.org and set your user name in the settings.",
        Res.IconInformation,
        false));

      return false;
    }

    public void NewGeoNameFromGps(string userName) {
      if (!IsGeoNamesUserNameInSettings(userName)) return;

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

      if (Core.DialogHostShow(inputDialog) != 0) return;

      var (lat, lng) = ParseLatLng(inputDialog.Answer);
      InsertGeoNameHierarchy(lat, lng, userName);
    }
  }
}
