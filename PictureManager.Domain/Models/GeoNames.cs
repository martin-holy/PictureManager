using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class GeoNames : BaseCatTreeViewCategory, ITable {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new List<IRecord>();
    public Dictionary<int, GeoName> AllDic { get; set; }

    public GeoNames() : base(Category.GeoNames) {
      Title = "GeoNames";
      IconName = IconName.LocationCheckin;
    }

    public void NewFromCsv(string csv) {
      // ID|Name|ToponymName|FCode|Parent
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var geoName = new GeoName(int.Parse(props[0]), props[1], props[2], props[3], null) {Csv = props};
      All.Add(geoName);
      AllDic.Add(geoName.Id, geoName);
    }

    public void LinkReferences() {
      // ID|Name|ToponymName|FCode|Parent
      foreach (var geoName in All.Cast<GeoName>()) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(geoName.Csv[4]))
          geoName.Parent = AllDic[int.Parse(geoName.Csv[4])];
        else 
          geoName.Parent = this;

        geoName.Parent.Items.Add(geoName);

        // csv array is not needed any more
        geoName.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic = new Dictionary<int, GeoName>();
      Helper.LoadFromFile();
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
          parentGeoName?.Items.Add((ICatTreeViewItem) dbGeoName);
          Core.Instance.RunOnUiThread(Core.Instance.Sdb.SetModified<GeoNames>);
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
  }
}
