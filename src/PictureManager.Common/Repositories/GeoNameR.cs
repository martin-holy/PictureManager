using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.TreeCategories;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace PictureManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Name|ToponymName|FCode|Parent
/// </summary>
public class GeoNameR : TreeDataAdapter<GeoNameM> {
  private bool _webLoadDisabled;

  public GeoNamesTreeCategory Tree { get; }
  public bool ApiLimitExceeded { get; set; }

  public GeoNameR() : base("GeoNames", 5) {
    Tree = new(this);
  }

  public override GeoNameM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], csv[2], csv[3], null);

  public override string ToCsv(GeoNameM geoName) =>
    string.Join("|",
      geoName.GetHashCode().ToString(),
      geoName.Name,
      geoName.ToponymName,
      geoName.Fcode,
      (geoName.Parent as GeoNameM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    Tree.Items.Clear();
    LinkTree(Tree, 4);
  }

  public GeoNameM ItemCreate(int id, string name, string toponymName, string fCode, ITreeItem parent) =>
    TreeItemCreate(new(id, name, toponymName, fCode, parent));

  public Task<GeoNameM> CreateGeoNameHierarchy(double lat, double lng) =>
    CreateGeoNameHierarchy($"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}".Replace(",", "."));

  public Task<GeoNameM> CreateGeoNameHierarchy(int id) =>
    CreateGeoNameHierarchy($"http://api.geonames.org/hierarchy?geonameId={id}");

  private async Task<GeoNameM> CreateGeoNameHierarchy(string url) {
    if (ApiLimitExceeded) return null;

    if (!Core.Settings.GeoName.LoadFromWeb) {
      if (!_webLoadDisabled) {
        _webLoadDisabled = true;
        Log.Error("Load GeoNames from web is disabled.", "Enable it in the settings.");
      }

      return null;
    }

    if (string.IsNullOrEmpty(Core.Settings.GeoName.UserName)) {
      ApiLimitExceeded = true;
      Log.Error("GeoNames user name was not set.", "Please register at geonames.org and set your user name in the settings.");
      return null;
    }

    try {
      var root = await Task.Run(() => {
        var xml = new XmlDocument();
        xml.Load($"{url}&username={Core.Settings.GeoName.UserName}");
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
        gn = All.SingleOrDefault(x => x.GetHashCode() == geoNameId) ??
             ItemCreate(
               geoNameId,
               g.SelectSingleNode("name")?.InnerText,
               g.SelectSingleNode("toponymName")?.InnerText,
               g.SelectSingleNode("fcode")?.InnerText,
               (ITreeItem)gn ?? Tree);
      }

      return gn;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  public static (double, double) ParseLatLng(string latLng) {
    try {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-"), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-"), CultureInfo.InvariantCulture);

      return (lat, lng);
    }
    catch {
      return (0, 0);
    }
  }
}