﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Common.Repositories;

public sealed class VideoItemsOrderR : OneToManyMultiDataAdapter<VideoM, VideoItemM> {
  private readonly CoreR _coreR;

  public VideoItemsOrderR(CoreR coreR) : base(coreR, "VideoItemsOrder", coreR.Video) {
    _coreR = coreR;
    IsDriveRelated = true;
    coreR.ReadyEvent += delegate { OnDbReady(); };
  }

  private void OnDbReady() {
    _coreR.VideoClip.ItemDeletedEvent += (_, e) => RemoveValueItem(e);
    _coreR.VideoImage.ItemDeletedEvent += (_, e) => RemoveValueItem(e);
    _coreR.VideoClip.ItemCreatedEvent += (_, e) => OnValueItemCreated(e);
    _coreR.VideoImage.ItemCreatedEvent += (_, e) => OnValueItemCreated(e);
  }

  private void RemoveValueItem(VideoItemM item) {
    if (!All.TryGetValue(item.Video, out var value)) return;
    if (!value.Remove(item)) return;
    IsModified = true;
  }

  private void OnValueItemCreated(VideoItemM item) {
    if (!All.TryGetValue(item.Video, out var value)) return;
    value.AddInOrder(item, (a, b) => a.TimeStart - b.TimeStart);
    IsModified = true;
  }

  public override Dictionary<string, IEnumerable<KeyValuePair<VideoM, List<VideoItemM>>>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Key.Folder);

  public override VideoItemM GetValueById(string id) {
    var intId = int.Parse(id);
    if (_coreR.VideoClip.AllDict.TryGetValue(intId, out var vc)) return vc;
    if (_coreR.VideoImage.AllDict.TryGetValue(intId, out var vi)) return vi;
    return null;
  }
}