using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PictureManager.ViewModel {
  public class GeoNames: BaseCategoryItem {
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
      foreach (var geoName in ACore.Db.GeoNames.Where(x => x.ParentGeoNameId == parent.GeoNameId)
        .Select(x => new GeoName(x) {Parent = parent})) {
        parent.Items.Add(geoName);
        AllGeoNames.Add(geoName);
        LoadChilds(geoName);
      }
    }

    public void New(string latLng) {
      const string url = "http://api.geonames.org/extendedFindNearby?lat={0}&lng={1}&username=cospi";
      var lat = latLng.Split(',')[0].Replace("N", "").Replace("S", "-");
      var lng = latLng.Split(',')[1].Replace("E", "").Replace("W", "-");

      var xml = new XmlDocument();
      xml.Load(string.Format(url, lat, lng));
      var geonames = xml.SelectNodes("/geonames/geoname");
      if (geonames == null) return;

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

          ACore.Db.InsertOnSubmit(dbGeoName);
        }

        parentGeoName = dbGeoName;
      }

      ACore.Db.SubmitChanges();

      Load();
    }
  }
}
