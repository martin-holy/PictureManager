using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class GeoNames : VM.BaseCategoryItem, ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public GeoNames() : base(Category.GeoNames) {
      Title = "GeoNames";
      IconName = IconName.LocationCheckin;
    }

    public void NewFromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 6) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new GeoName(id, props[1], props[2], props[3], null) { Csv = props });
    }

    public void LinkReferences(SimpleDB sdb) {
      foreach (var item in Records) {
        var geoName = (GeoName)item.Value;

        // reference to parent
        if (geoName.Csv[4] != string.Empty)
          geoName.Parent = (GeoName)Records[int.Parse(geoName.Csv[4])];

        // reference to children
        if (geoName.Csv[5] != string.Empty)
          foreach (var geoNameId in geoName.Csv[5].Split(','))
            geoName.Items.Add((GeoName)Records[int.Parse(geoNameId)]);

        // csv array is not needed any more
        geoName.Csv = null;
      }
    }

    public void Load() {
      Items.Clear();

      var earth = Records.Cast<GeoName>().SingleOrDefault(x => x.Parent == null);
      if (earth == null) return;
      Items.Add(earth);
    }

    public GeoName InsertGeoNameHierarchy(double lat, double lng) {
      var url = $"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}&username=cospi".Replace(",", ".");
      var xml = new XmlDocument();
      xml.Load(string.Format(url, lat, lng));
      var geonames = xml.SelectNodes("/geonames/geoname");
      if (geonames == null) return null;

      GeoName parentGeoName = null;
      foreach (XmlNode geoname in geonames) {
        var geoNameId = int.Parse(geoname.SelectSingleNode("geonameId")?.InnerText ?? "0");

        if (!Records.TryGetValue(geoNameId, out var dbGeoName)) {
          var id = ACore.Sdb.Table<GeoNames>().GetNextId();
          dbGeoName = new GeoName(
            id,
            geoname.SelectSingleNode("name")?.InnerText,
            geoname.SelectSingleNode("toponymName")?.InnerText,
            geoname.SelectSingleNode("fcode")?.InnerText,
            parentGeoName);
        }

        parentGeoName = (GeoName)dbGeoName;
      }

      return parentGeoName;
    }

    public void New(string latLng) {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-").Replace(",", "."), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-").Replace(",", "."), CultureInfo.InvariantCulture);

      //TODO InsertGeoNameHierarchy nemusi nic vracet a Load asi nebude potreba
      InsertGeoNameHierarchy(lat, lng);

      Load();
    }

    public string GetGeoNameHierarchy(int? geoNameId) {
      if (geoNameId == null) return string.Empty;

      if (!Records.TryGetValue((int)geoNameId, out var geoName))
        return string.Empty;

      var parent = ((GeoName)geoName).Parent;
      var names = new List<string> { ((GeoName)geoName).Title };
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent;
      }

      names.Reverse();

      return string.Join("\n", names);
    }
  }
}
