using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Database;

public sealed class MediaItemGeoLocationDA : OneToOneDataAdapter<MediaItemM, GeoLocationM> {
  public MediaItemGeoLocationDA(Db db) : base("MediaItemGeoLocation", db, db.MediaItems, db.GeoLocations) {
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<KeyValuePair<MediaItemM, GeoLocationM>>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Key.Folder);
}