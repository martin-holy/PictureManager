using System.Collections.Generic;
using System.Linq;

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
  }
}
