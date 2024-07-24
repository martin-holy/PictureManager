﻿using MH.Utils.BaseClasses;
using PictureManager.Common.Features.GeoLocation;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemGeoLocationR : OneToOneDataAdapter<MediaItemM, GeoLocationM> {
  public MediaItemGeoLocationR(CoreR coreR) : base(coreR, "MediaItemGeoLocation", coreR.MediaItem, coreR.GeoLocation) {
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<KeyValuePair<MediaItemM, GeoLocationM>>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(
      DataAdapterA
        .GetAll(x => x.GeoLocation != null)
        .Select(x => new KeyValuePair<MediaItemM, GeoLocationM>(x, x.GeoLocation!)),
      x => x.Key.Folder);

  public override void AddItem(KeyValuePair<MediaItemM, GeoLocationM> item, string[] props) =>
    item.Key.GeoLocation = item.Value;

  public void ItemUpdate(KeyValuePair<MediaItemM, GeoLocationM?> item) {
    if (ReferenceEquals(item.Key.GeoLocation, item.Value)) return;
    item.Key.GeoLocation = item.Value;
    IsModified = true;
    DataAdapterA.Modify(item.Key);
  }
}