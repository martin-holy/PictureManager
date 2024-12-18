﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem.Video;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class VideoR : TableDataAdapter<VideoM> {
  public static VideoM Dummy = new(0, FolderR.Dummy, string.Empty);
  private readonly CoreR _coreR;

  public VideoR(CoreR coreR) : base(coreR, "Videos", 11) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  protected override Dictionary<string, IEnumerable<VideoM>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  protected override VideoM _fromCsv(string[] csv) =>
    new(int.Parse(csv[0]), FolderR.Dummy, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = (Orientation)csv[5].IntParseOrDefault(1),
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
      IsOnlyInDb = csv[10] == "1"
    };

  protected override string _toCsv(VideoM vid) =>
    string.Join("|",
      vid.GetHashCode().ToString(),
      vid.Folder.GetHashCode().ToString(),
      vid.FileName,
      vid.Width.ToString(),
      vid.Height.ToString(),
      vid.Orientation.ToInt().ToString(),
      vid.Rating.ToString(),
      vid.Comment ?? string.Empty,
      vid.People.ToHashCodes().ToCsv(),
      vid.Keywords.ToHashCodes().ToCsv(),
      vid.IsOnlyInDb ? "1" : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in _allCsv) {
      mi.Folder = _coreR.Folder.GetById(csv[1])!;
      mi.Folder.MediaItems.Add(mi);
      mi.People = _coreR.Person.Link(csv[8], this);
      mi.Keywords = _coreR.Keyword.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public VideoM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  protected override void _onItemDeleted(object sender, VideoM item) {
    _coreR.VideoClip.ItemsDelete(item.VideoClips?.ToArray());
    _coreR.VideoImage.ItemsDelete(item.VideoImages?.ToArray());
    _coreR.MediaItem.OnItemDeletedCommon(item);
  }
}