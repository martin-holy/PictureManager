using MH.Utils.BaseClasses;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Database;

public sealed class VideoItemsOrderDA : OneToManyMultiDataAdapter<VideoM, VideoItemM> {
  private readonly Db _db;

  public VideoItemsOrderDA(Db db) : base("VideoItemsOrder", db, db.Videos) {
    _db = db;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<KeyValuePair<VideoM, List<VideoItemM>>>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Key.Folder);

  public override VideoItemM GetValueById(string id) {
    var intId = int.Parse(id);
    if (_db.VideoClips.AllDict.TryGetValue(intId, out var vc)) return vc;
    if (_db.VideoImages.AllDict.TryGetValue(intId, out var vi)) return vi;
    return null;
  }
}