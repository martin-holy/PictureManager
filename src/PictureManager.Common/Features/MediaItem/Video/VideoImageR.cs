using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem.Video;

/// <summary>
/// DB fields: ID|MediaItem|Time|Rating|Comment|People|Keywords
/// </summary>
public class VideoImageR : TableDataAdapter<VideoImageM> {
  private readonly CoreR _coreR;

  public VideoImageR(CoreR coreR) : base(coreR, "VideoImages", 7) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  protected override Dictionary<string, IEnumerable<VideoImageM>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  protected override VideoImageM _fromCsv(string[] csv) =>
    new(int.Parse(csv[0]), VideoR.Dummy, csv[2].IntParseOrDefault(0)) {
      Rating = csv[3].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[4]) ? null : csv[4]
    };

  protected override string _toCsv(VideoImageM vi) =>
    string.Join("|",
      vi.GetHashCode().ToString(),
      vi.Video.GetHashCode().ToString(),
      vi.TimeStart.ToString(),
      vi.Rating.ToString(),
      vi.Comment ?? string.Empty,
      vi.People.ToHashCodes().ToCsv(),
      vi.Keywords.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    foreach (var (vi, csv) in AllCsv) {
      vi.Video = _coreR.Video.GetById(csv[1])!;
      vi.Video.VideoImages ??= [];
      vi.Video.VideoImages.Add(vi);
      vi.People = _coreR.Person.Link(csv[5], this);
      vi.Keywords = _coreR.Keyword.Link(csv[6], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public VideoImageM CustomItemCreate(VideoM video, int timeStart) =>
    ItemCreate(new(GetNextId(), video, timeStart));

  protected override void _onItemCreated(object sender, VideoImageM item) =>
    item.Video.Toggle(item);

  protected override void _onItemDeleted(object sender, VideoImageM item) {
    _coreR.MediaItem.OnItemDeletedCommon(item);
    item.Video.Toggle(item);
  }
}