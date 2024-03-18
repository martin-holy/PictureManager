using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class ImageR : TableDataAdapter<ImageM> {
  private readonly CoreR _coreR;

  public ImageR(CoreR coreR) : base("Images", 11) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<ImageM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  public override ImageM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = (Orientation)csv[5].IntParseOrDefault(1),
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
      img.Orientation.ToInt().ToString(),
      img.Rating.ToString(),
      img.Comment ?? string.Empty,
      img.People.ToHashCodes().ToCsv(),
      img.Keywords.ToHashCodes().ToCsv(),
      img.IsOnlyInDb ? "1" : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in AllCsv) {
      mi.Folder = _coreR.Folder.GetById(csv[1]);
      mi.Folder.MediaItems.Add(mi);
      mi.People = _coreR.Person.Link(csv[8], this);
      mi.Keywords = _coreR.Keyword.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public ImageM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  protected override void OnItemDeleted(ImageM item) {
    _coreR.MediaItem.OnItemDeletedCommon(item);
  }
}