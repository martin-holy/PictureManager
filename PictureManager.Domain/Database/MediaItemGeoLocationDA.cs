using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

public sealed class MediaItemGeoLocationDA : OneToOneDataAdapter<MediaItemM, GeoLocationM> {
  public MediaItemGeoLocationDA(Db db) : base("MediaItemGeoLocation", db.MediaItems, db.GeoLocations) {
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<KeyValuePair<MediaItemM, GeoLocationM>>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(
      DataAdapterA
        .GetAll(x => x.GeoLocation != null)
        .Select(x => new KeyValuePair<MediaItemM, GeoLocationM>(x, x.GeoLocation)),
      x => x.Key.Folder);

  public override void AddItem(KeyValuePair<MediaItemM, GeoLocationM> item, string[] props) =>
    item.Key.GeoLocation = item.Value;

  public void ItemUpdate(KeyValuePair<MediaItemM, GeoLocationM> item) {
    if (ReferenceEquals(item.Key.GeoLocation, item.Value)) return;
    item.Key.GeoLocation = item.Value;
    IsModified = true;
    DataAdapterA.Modify(item.Key);
  }
}