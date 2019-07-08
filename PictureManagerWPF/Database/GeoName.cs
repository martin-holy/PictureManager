using System.Collections.Generic;
using System.Linq;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class GeoName : VM.BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; } // this is GeoNameId not just DB Id
    public string ToponymName { get; set; }
    public string Fcode { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

    public GeoName(int id, string name, string toponymName, string fCode, GeoName parent) {
      Id = id;
      Title = name;
      ToponymName = toponymName;
      Fcode = fCode;
      Parent = parent;
      IconName = IconName.LocationCheckin;
    }

    public string ToCsv() {
      // ID|Name|ToponymName|FCode|Parent|Children
      return string.Join("|",
        Id.ToString(),
        Title,
        ToponymName,
        Fcode,
        ((IRecord)Parent)?.Id.ToString(),
        string.Join(",", Items.Cast<IRecord>().Select(x => x.Id)));
    }

    public BaseMediaItem[] GetMediaItems(bool recursive) {
      return recursive ? GetMediaItemsRecursive() : MediaItems.ToArray();
    }

    public void GetThisAndItems(ref List<GeoName> geoNames) {
      geoNames.Add(this);
      foreach (var geoName in Items) {
        ((GeoName)geoName).GetThisAndItems(ref geoNames);
      }
    }

    public BaseMediaItem[] GetMediaItemsRecursive() {
      // get all GeoNames
      var geoNames = new List<GeoName>();
      GetThisAndItems(ref geoNames);

      // get all MediaItems from geoNames
      var mis = new List<BaseMediaItem>();
      foreach (var gn in geoNames)
        mis.AddRange(gn.MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
