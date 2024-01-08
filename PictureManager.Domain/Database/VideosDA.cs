using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class VideosDA : TableDataAdapter<VideoM> {
  private readonly Db _db;

  public VideosDA(Db db) : base("Videos", 11) {
    _db = db;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<VideoM>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Folder);

  public override VideoM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = csv[5].IntParseOrDefault(1),
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
      IsOnlyInDb = csv[10] == "1"
    };

  public override string ToCsv(VideoM vid) =>
    string.Join("|",
      vid.GetHashCode().ToString(),
      vid.Folder.GetHashCode().ToString(),
      vid.FileName,
      vid.Width.ToString(),
      vid.Height.ToString(),
      vid.Orientation.ToString(),
      vid.Rating.ToString(),
      vid.Comment ?? string.Empty,
      vid.People.ToHashCodes().ToCsv(),
      vid.Keywords.ToHashCodes().ToCsv(),
      vid.IsOnlyInDb ? "1" : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in AllCsv) {
      mi.Folder = _db.Folders.AllDict[int.Parse(csv[1])];
      mi.Folder.MediaItems.Add(mi);
      mi.People = _db.People.Link(csv[8], this);
      mi.Keywords = _db.Keywords.Link(csv[9], this);
    }
  }

  public override void Modify(VideoM item) {
    base.Modify(item);
    item.IsOnlyInDb = true;
  }

  public override int GetNextId() =>
    _db.MediaItems.GetNextId();

  public VideoM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  public VideoM ItemCopy(VideoM item, FolderM folder, string fileName) {
    var copy = ItemCreate(folder, fileName);
    _db.MediaItems.ItemCopyCommon(item, copy);
    return copy;
  }

  protected override void OnItemDeleted(VideoM item) {
    _db.MediaItems.OnItemDeletedCommon(item);
  }
}