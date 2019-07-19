using System.Collections.Generic;
using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class GeoName : BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; } // this is GeoNameId not just DB Id
    public string ToponymName { get; set; }
    public string Fcode { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

    public GeoName(int id, string name, string toponymName, string fCode, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      ToponymName = toponymName;
      Fcode = fCode;
      Parent = parent;
      IconName = IconName.LocationCheckin;
    }

    public string ToCsv() {
      // ID|Name|ToponymName|FCode|Parent
      return string.Join("|",
        Id.ToString(),
        Title,
        ToponymName,
        Fcode,
        (Parent as GeoName)?.Id.ToString());
    }

    public BaseMediaItem[] GetMediaItems(bool recursive) {
      return recursive ? GetMediaItemsRecursive() : MediaItems.ToArray();
    }

    public BaseMediaItem[] GetMediaItemsRecursive() {
      // get all GeoNames
      var geoNames = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref geoNames);

      // get all MediaItems from geoNames
      var bmis = new List<BaseMediaItem>();
      foreach (var gn in geoNames.Cast<GeoName>())
        bmis.AddRange(gn.MediaItems);

      return bmis.Distinct().ToArray();
    }
  }
}
