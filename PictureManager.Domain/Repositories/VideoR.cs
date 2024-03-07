using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Repositories;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class VideoR : TableDataAdapter<VideoM> {
  private readonly CoreR _coreR;

  public VideoR(CoreR coreR) : base("Videos", 11) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<VideoM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.Folder);

  public override VideoM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = (Orientation)csv[5].IntParseOrDefault(1),
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
      vid.Orientation.ToInt().ToString(),
      vid.Rating.ToString(),
      vid.Comment ?? string.Empty,
      vid.People.ToHashCodes().ToCsv(),
      vid.Keywords.ToHashCodes().ToCsv(),
      vid.IsOnlyInDb ? "1" : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in AllCsv) {
      mi.Folder = _coreR.Folder.AllDict[int.Parse(csv[1])];
      mi.Folder.MediaItems.Add(mi);
      mi.People = _coreR.Person.Link(csv[8], this);
      mi.Keywords = _coreR.Keyword.Link(csv[9], this);
    }
  }

  public override int GetNextId() =>
    _coreR.MediaItem.GetNextId();

  public VideoM ItemCreate(FolderM folder, string fileName) =>
    ItemCreate(new(GetNextId(), folder, fileName));

  protected override void OnItemDeleted(VideoM item) {
    _coreR.VideoClip.ItemsDelete(item.VideoClips.ToArray());
    _coreR.VideoImage.ItemsDelete(item.VideoImages.ToArray());
    _coreR.MediaItem.OnItemDeletedCommon(item);
  }
}