using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class GeoNames : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<GeoName> All { get; } = new List<GeoName>();
    public Dictionary<int, GeoName> AllDic { get; } = new Dictionary<int, GeoName>();

    public GeoNames() : base(Category.GeoNames) {
      Title = "GeoNames";
      IconName = IconName.LocationCheckin;
    }

    public void NewFromCsv(string csv) {
      // ID|Name|ToponymName|FCode|Parent
      var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      AddRecord(new GeoName(id, props[1], props[2], props[3], null) { Csv = props });
    }

    public void LinkReferences() {
      // ID|Name|ToponymName|FCode|Parent
      foreach (var geoName in All) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(geoName.Csv[4])) {
          geoName.Parent = AllDic[int.Parse(geoName.Csv[4])];
          geoName.Parent.Items.Add(geoName);
        }

        // csv array is not needed any more
        geoName.Csv = null;
      }

      Items.Clear();

      var earth = All.SingleOrDefault(x => x.Parent == null);
      if (earth == null) return;
      Items.Add(earth);
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic.Clear();
      Helper.LoadFromFile();
    }

    private void AddRecord(GeoName record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
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

        if (!AllDic.TryGetValue(geoNameId, out var dbGeoName)) {
          dbGeoName = new GeoName(
            geoNameId,
            geoname.SelectSingleNode("name")?.InnerText,
            geoname.SelectSingleNode("toponymName")?.InnerText,
            geoname.SelectSingleNode("fcode")?.InnerText,
            parentGeoName);

          AddRecord(dbGeoName);
          Helper.IsModified = true;
        }

        parentGeoName = dbGeoName;
      }

      return parentGeoName;
    }

    public void New(string latLng) {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-").Replace(",", "."), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-").Replace(",", "."), CultureInfo.InvariantCulture);

      InsertGeoNameHierarchy(lat, lng);
      SaveToFile();
    }
  }
}
