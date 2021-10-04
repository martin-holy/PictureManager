using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace PictureManager.Domain.Models {
  public sealed class GeoNames : BaseCatTreeViewCategory, ITable {
    private readonly Core _core;
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, GeoName> AllDic { get; set; }

    public GeoNames(Core core) : base(Category.GeoNames) {
      _core = core;
      DataAdapter = new GeoNamesDataAdapter(core, this);
      Title = "GeoNames";
      IconName = IconName.LocationCheckin;
    }

    public GeoName InsertGeoNameHierarchy(double lat, double lng, string userName) {
      var url = $"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}&username={userName}".Replace(",", ".");
      var xml = new XmlDocument();
      xml.Load(url);
      var geonames = xml.SelectNodes("/geonames/geoname");
      if (geonames == null) return null;

      GeoName parentGeoName = null;
      foreach (XmlNode geoname in geonames) {
        var geoNameId = int.Parse(geoname.SelectSingleNode("geonameId")?.InnerText ?? "0");
        var dbGeoName = All.SingleOrDefault(x => x.Id == geoNameId);

        if (dbGeoName == null) {
          dbGeoName = new GeoName(
            geoNameId,
            geoname.SelectSingleNode("name")?.InnerText,
            geoname.SelectSingleNode("toponymName")?.InnerText,
            geoname.SelectSingleNode("fcode")?.InnerText,
            parentGeoName);

          All.Add(dbGeoName);
          parentGeoName?.Items.Add((ICatTreeViewItem)dbGeoName);
          _core.RunOnUiThread(() => DataAdapter.IsModified = true);
        }

        parentGeoName = dbGeoName as GeoName;
      }

      return parentGeoName;
    }

    public void New(string latLng, string userName) {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-").Replace(",", "."), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-").Replace(",", "."), CultureInfo.InvariantCulture);

      InsertGeoNameHierarchy(lat, lng, userName);
    }

    /// <summary>
    /// Toggle GeoName on Media Item
    /// </summary>
    /// <param name="g">GeoName</param>
    /// <param name="mi">MediaItem</param>
    public static void Toggle(GeoName g, MediaItem mi) {
      mi.GeoName?.MediaItems.Remove(mi);
      mi.GeoName = g;
      g.MediaItems.Add(mi);
    }
  }
}
