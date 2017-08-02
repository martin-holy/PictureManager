using System.Collections.Generic;

namespace PictureManager.ViewModel {
  public class GeoName : BaseTreeViewTagItem {
    public int GeoNameId;
    public DataModel.GeoName Data;

    public GeoName(DataModel.GeoName data) {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      GeoNameId = data.GeoNameId;
      IconName = "appbar_location_checkin";
    }

    public void GetThisAndSubGeoNames(ref List<GeoName> geoNames) {
      geoNames.Add(this);
      foreach (var geoName in Items) {
        ((GeoName) geoName).GetThisAndSubGeoNames(ref geoNames);
      }
    }
  }
}