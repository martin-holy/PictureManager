using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem.Image;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class ImageR : TableDataAdapter<ImageM> {
  private readonly CoreR _coreR;

  public ImageR(CoreR coreR) : base(coreR, "Images", 11) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  protected override Dictionary<string, IEnumerable<ImageM>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  protected override ImageM _fromCsv(string[] csv) =>
    new(int.Parse(csv[0]), FolderR.Dummy, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = (Imaging.Orientation)csv[5].IntParseOrDefault(1),
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
      IsOnlyInDb = csv[10] == "1"
    };

  protected override string _toCsv(ImageM img) =>
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
    foreach (var (mi, csv) in _allCsv) {
      mi.Folder = _coreR.Folder.GetById(csv[1])!;
      mi.Folder.MediaItems.Add(mi);
      mi.People = _coreR.Person.Link(csv[8], this);
      mi.Keywords = _coreR.Keyword.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public ImageM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  protected override void _onItemDeleted(object sender, ImageM item) {
    _coreR.MediaItem.OnItemDeletedCommon(item);
  }
}