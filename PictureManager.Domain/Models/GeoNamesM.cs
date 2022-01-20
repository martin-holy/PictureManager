using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class GeoNamesM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly Core _core;

    public DataAdapter DataAdapter { get; set; }
    public List<GeoNameM> All { get; } = new();
    public Dictionary<int, GeoNameM> AllDic { get; set; }

    public GeoNamesM(Core core) {
      _core = core;
    }

    public GeoNameM InsertGeoNameHierarchy(double lat, double lng, string userName) {
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
          _core.RunOnUiThread(() => DataAdapter.IsModified = true);
        }

        parentGeoName = dbGeoName;
      }

      return parentGeoName;
    }

    public void New(string latLng, string userName) {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-").Replace(",", "."), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-").Replace(",", "."), CultureInfo.InvariantCulture);

      InsertGeoNameHierarchy(lat, lng, userName);
    }
  }
}
