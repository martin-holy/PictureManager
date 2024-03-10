using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Repositories;

/// <summary>
/// DB fields: ID|MediaItem|TimeStart|TimeEnd|Volume|Speed|Rating|Comment|People|Keywords
/// </summary>
public class VideoClipR : TableDataAdapter<VideoClipM> {
  private readonly CoreR _coreR;

  public VideoClipR(CoreR coreR) : base("VideoClips", 10) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<VideoClipM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  public override VideoClipM FromCsv(string[] csv) {
    var vc = new VideoClipM(int.Parse(csv[0]), null, csv[2].IntParseOrDefault(0)) {
      TimeEnd = csv[3].IntParseOrDefault(0),
      Volume = csv[4].IntParseOrDefault(50) / 100.0,
      Speed = csv[5].IntParseOrDefault(10) / 10.0,
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7]
    };

    return vc;
  }

  public override string ToCsv(VideoClipM vc) =>
    string.Join("|",
      vc.GetHashCode().ToString(),
      vc.Video.GetHashCode().ToString(),
      vc.TimeStart.ToString(),
      vc.TimeEnd.ToString(),
      ((int)(vc.Volume * 100)).ToString(),
      ((int)(vc.Speed * 10)).ToString(),
      vc.Rating.ToString(),
      vc.Comment ?? string.Empty,
      vc.People.ToHashCodes().ToCsv(),
      vc.Keywords.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    foreach (var (vc, csv) in AllCsv) {
      vc.Video = _coreR.Video.GetById(csv[1]);
      vc.Video.VideoClips ??= new();
      vc.Video.VideoClips.Add(vc);
      vc.People = _coreR.Person.Link(csv[8], this);
      vc.Keywords = _coreR.Keyword.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public VideoClipM CustomItemCreate(VideoM video, int timeStart) =>
    ItemCreate(new(GetNextId(), video, timeStart));

  protected override void OnItemCreated(VideoClipM item) {
    item.Video.Toggle(item);
  }

  protected override void OnItemDeleted(VideoClipM item) {
    _coreR.MediaItem.OnItemDeletedCommon(item);
    item.Video.Toggle(item);
    item.Video = null;
  }
}