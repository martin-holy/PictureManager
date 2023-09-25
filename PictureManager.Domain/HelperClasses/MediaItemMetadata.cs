using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.HelperClasses {
  public class MediaItemMetadata {
    public MediaItemM MediaItem { get; }
    public bool Success { get; set; }
    public string[] People { get; set; }
    public string[] Keywords { get; set; }
    public string GeoName { get; set; }

    public MediaItemMetadata(MediaItemM mediaItem) {
      MediaItem = mediaItem;
    }

    public void FindRefs() {
      MediaItem.People = null;
      if (People != null) {
        MediaItem.People = new(People.Length);
        foreach (var person in People)
          MediaItem.People.Add(Core.Db.People.GetPerson(person, true));
      }

      MediaItem.Keywords = null;
      if (Keywords != null) {
        MediaItem.Keywords = new();
        foreach (var k in Keywords.OrderByDescending(x => x).Distinct()) {
          var keyword = Core.KeywordsM.GetByFullPath(k.Replace('|', ' '));
          if (keyword != null)
            MediaItem.Keywords.Add(keyword);
        }
      }

      if (!string.IsNullOrEmpty(GeoName)) {
        // TODO find/create GeoName
        MediaItem.GeoName = Core.Db.GeoNames.All
          .SingleOrDefault(x => x.GetHashCode() == int.Parse(GeoName));
      }
    }
  }
}
