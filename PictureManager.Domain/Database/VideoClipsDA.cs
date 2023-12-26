using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|MediaItem|TimeStart|TimeEnd|Volume|Speed|Rating|Comment|People|Keywords
/// </summary>
public class VideoClipsDA : TableDataAdapter<VideoClipM> {
  private readonly Db _db;

  public VideoClipsM Model { get; }

  public VideoClipsDA(Db db) : base("VideoClips", 10) {
    _db = db;
    IsDriveRelated = true;
    Model = new(this);
  }

  public override Dictionary<string, IEnumerable<VideoClipM>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Folder);

  public override VideoClipM FromCsv(string[] csv) {
    var vc = new VideoClipM(int.Parse(csv[0]), null) {
      TimeStart = csv[2].IntParseOrDefault(0),
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
      vc.Video = _db.Videos.GetById(csv[1]);
      vc.Video.HasVideoClips = true;
      vc.People = _db.People.Link(csv[8], this);
      vc.Keywords = _db.Keywords.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _db.MediaItems.GetNextId();

  public VideoClipM ItemCreate() =>
    ItemCreate(new(GetNextId(), _db.Videos.Model.Current));

  protected override void OnItemDeleted(VideoClipM item) {
    File.Delete(item.FilePathCache);
    item.Video.HasVideoClips = _db.VideoClips.All.Any(x => ReferenceEquals(x.Video, item.Video));
    item.Video = null;
    item.People = null;
    item.Keywords = null;
  }
}