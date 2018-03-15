using System.Collections.Generic;

namespace PictureManager.ViewModel {
  public class GeoName : BaseTreeViewTagItem, IDbItem {
    public DataModel.GeoName Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }

    public GeoName(DataModel.GeoName data) {
      Data = data;
      IconName = IconName.LocationCheckin;
    }

    public void GetThisAndSubGeoNames(ref List<GeoName> geoNames) {
      geoNames.Add(this);
      foreach (var geoName in Items) {
        ((GeoName) geoName).GetThisAndSubGeoNames(ref geoNames);
      }
    }
  }
}