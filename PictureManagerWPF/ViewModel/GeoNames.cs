using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace PictureManager.ViewModel {
  public sealed class GeoNames: BaseCategoryItem {
    public List<GeoName> AllGeoNames;

    public GeoNames() : base (Categories.GeoNames) {
      AllGeoNames = new List<GeoName>();
      Title = "GeoNames";
      IconName = "appbar_location_checkin";
    }

    public void Load() {
      Items.Clear();
      AllGeoNames.Clear();

      var earth = ACore.Db.GeoNames.SingleOrDefault(x => x.ParentGeoNameId == null);
      if (earth == null) return;
      var geoName = new GeoName(earth);
      Items.Add(geoName);
      AllGeoNames.Add(geoName);
      LoadChilds(geoName);
    }

    private void LoadChilds(GeoName parent) {
      foreach (var geoName in ACore.Db.GeoNames.Where(x => x.ParentGeoNameId == parent.Data.GeoNameId)
        .Select(x => new GeoName(x) {Parent = parent})) {
        parent.Items.Add(geoName);
        AllGeoNames.Add(geoName);
        LoadChilds(geoName);
      }
    }

    public DataModel.GeoName InsertGeoNameHierarchy(double lat, double lng) {
      var url = $"http://api.geonames.org/extendedFindNearby?lat={lat}&lng={lng}&username=cospi".Replace(",", ".");

      var xml = new XmlDocument();
      xml.Load(string.Format(url, lat, lng));
      var geonames = xml.SelectNodes("/geonames/geoname");
      if (geonames == null) return null;

      var lists = ACore.Db.GetInsertUpdateDeleteLists();
      DataModel.GeoName parentGeoName = null;
      foreach (XmlNode geoname in geonames) {
        var geoNameId = int.Parse(geoname.SelectSingleNode("geonameId")?.InnerText ?? "0");
        var dbGeoName = ACore.Db.GeoNames.SingleOrDefault(x => x.GeoNameId == geoNameId);

        if (dbGeoName == null) {
          dbGeoName = new DataModel.GeoName {
            Id = ACore.Db.GetNextIdFor<DataModel.GeoName>(),
            GeoNameId = geoNameId,
            ToponymName = geoname.SelectSingleNode("toponymName")?.InnerText,
            Name = geoname.SelectSingleNode("name")?.InnerText,
            Fcode = geoname.SelectSingleNode("fcode")?.InnerText,
            ParentGeoNameId = parentGeoName?.GeoNameId
          };

          ACore.Db.InsertOnSubmit(dbGeoName, lists);
        }

        parentGeoName = dbGeoName;
      }

      ACore.Db.SubmitChanges(lists);
      return parentGeoName;
    }

    public void New(string latLng) {
      var lat = double.Parse(latLng.Split(',')[0].Replace("N", "").Replace("S", "-").Replace(",", "."), CultureInfo.InvariantCulture);
      var lng = double.Parse(latLng.Split(',')[1].Replace("E", "").Replace("W", "-").Replace(",", "."), CultureInfo.InvariantCulture);

      InsertGeoNameHierarchy(lat, lng);

      Load();
    }
  }
}
