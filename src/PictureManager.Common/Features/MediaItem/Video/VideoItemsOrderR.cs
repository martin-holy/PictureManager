using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem.Video;

public sealed class VideoItemsOrderR : OneToManyMultiDataAdapter<VideoM, VideoItemM> {
  private readonly CoreR _coreR;

  public VideoItemsOrderR(CoreR coreR) : base(coreR, "VideoItemsOrder", coreR.Video) {
    _coreR = coreR;
    IsDriveRelated = true;
    coreR.ReadyEvent += delegate { _onDbReady(); };
  }

  private void _onDbReady() {
    _coreR.VideoClip.ItemDeletedEvent += (_, e) => _onValueItemDeleted(e);
    _coreR.VideoImage.ItemDeletedEvent += (_, e) => _onValueItemDeleted(e);
    _coreR.VideoClip.ItemCreatedEvent += (_, e) => _onValueItemCreated(e);
    _coreR.VideoImage.ItemCreatedEvent += (_, e) => _onValueItemCreated(e);
  }

  private void _onValueItemDeleted(VideoItemM item) {
    if (!All.TryGetValue(item.Video, out var value)) return;
    if (!value.Remove(item)) return;
    IsModified = true;
  }

  private void _onValueItemCreated(VideoItemM item) {
    if (!All.TryGetValue(item.Video, out var value)) return;
    value.AddInOrder(item, (a, b) => a.TimeStart - b.TimeStart);
    IsModified = true;
  }

  protected override Dictionary<string, IEnumerable<KeyValuePair<VideoM, List<VideoItemM>>>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Key.Folder);

  public override VideoItemM? GetValueById(string id) {
    var intId = int.Parse(id);
    if (_coreR.VideoClip.AllDict.TryGetValue(intId, out var vc)) return vc;
    if (_coreR.VideoImage.AllDict.TryGetValue(intId, out var vi)) return vi;
    return null;
  }
}