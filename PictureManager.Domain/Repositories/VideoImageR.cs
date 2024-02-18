using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Repositories;

/// <summary>
/// DB fields: ID|MediaItem|Time|Rating|Comment|People|Keywords
/// </summary>
public class VideoImageR : TableDataAdapter<VideoImageM> {
  private readonly CoreR _coreR;

  public VideoImageR(CoreR coreR) : base("VideoImages", 7) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<VideoImageM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  public override VideoImageM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2].IntParseOrDefault(0)) {
      Rating = csv[3].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[4]) ? null : csv[4]
    };

  public override string ToCsv(VideoImageM vi) =>
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
      vi.Video = _coreR.Video.GetById(csv[1]);
      vi.Video.VideoImages ??= new();
      vi.Video.VideoImages.Add(vi);
      vi.People = _coreR.Person.Link(csv[5], this);
      vi.Keywords = _coreR.Keyword.Link(csv[6], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public VideoImageM CustomItemCreate(VideoM video, int timeStart) =>
    ItemCreate(new(GetNextId(), video, timeStart));

  protected override void OnItemCreated(VideoImageM item) =>
    item.Video.Toggle(item);

  protected override void OnItemDeleted(VideoImageM item) {
    _coreR.MediaItem.OnItemDeletedCommon(item);
    item.Video.Toggle(item);
    item.Video = null;
  }
}