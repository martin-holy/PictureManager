using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class ImagesDA : TableDataAdapter<ImageM> {
  private readonly Db _db;

  public ImagesM Model { get; }

  public ImagesDA(Db db) : base("Images", 11) {
    _db = db;
    IsDriveRelated = true;
    Model = new(this);
  }

  public override Dictionary<string, IEnumerable<ImageM>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Folder);

  public override ImageM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = csv[5].IntParseOrDefault(1),
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
      IsOnlyInDb = csv[10] == "1"
    };

  public override string ToCsv(ImageM img) =>
    string.Join("|",
      img.GetHashCode().ToString(),
      img.Folder.GetHashCode().ToString(),
      img.FileName,
      img.Width.ToString(),
      img.Height.ToString(),
      img.Orientation.ToString(),
      img.Rating.ToString(),
      img.Comment ?? string.Empty,
      img.People.ToHashCodes().ToCsv(),
      img.Keywords.ToHashCodes().ToCsv(),
      img.IsOnlyInDb ? "1" : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in AllCsv) {
      mi.Folder = _db.Folders.GetById(csv[1]);
      mi.Folder.MediaItems.Add(mi);
      mi.People = _db.People.Link(csv[8], this);
      mi.Keywords = _db.Keywords.Link(csv[9], this);
    }
  }

  public override void Modify(ImageM item) {
    base.Modify(item);
    item.IsOnlyInDb = true;
  }

  public override int GetNextId() =>
    _db.MediaItems.GetNextId();

  public ImageM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  public ImageM ItemCopy(ImageM item, FolderM folder, string fileName) {
    var copy = ItemCreate(folder, fileName);
    _db.MediaItems.ItemCopyCommon(item, copy);
    return copy;
  }

  protected override void OnItemDeleted(ImageM item) {
    _db.MediaItems.OnItemDeletedCommon(item);
  }
}