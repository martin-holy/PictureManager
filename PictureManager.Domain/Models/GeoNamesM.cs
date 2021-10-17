using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Utils;

namespace PictureManager.Domain.Models {
  public sealed class GeoNamesM : ITreeBranch {
    #region ITreeBranch implementation
    public object Parent { get; set; }
    public ObservableCollection<object> Items { get; set; } = new();
    #endregion

    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<GeoNameM> All { get; } = new();
    public Dictionary<int, GeoNameM> AllDic { get; set; }

    public GeoNamesM(Core core) {
      _core = core;
      DataAdapter = new GeoNamesDataAdapter(core, this);
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

    public IEnumerable<MediaItem> GetMediaItems(GeoNameM geoName, bool recursive) {
      var geoNames = new List<GeoNameM> { geoName };
      if (recursive) Tree.GetThisAndItemsRecursive(geoName, ref geoNames);
      var set = new HashSet<GeoNameM>(geoNames);

      return _core.MediaItems.All.Cast<MediaItem>().Where(mi => set.Contains(mi.GeoName));
    }
  }
}
