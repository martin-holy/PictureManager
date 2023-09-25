using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.Database;

// TODO move DB related code from MediaItemsM here

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
/// </summary>
public class MediaItemsDataAdapter : DataAdapter<MediaItemM> {

  public MediaItemsDataAdapter() : base("MediaItems", 12) { }

  public override void Save() =>
    SaveDriveRelated(All
      .GroupBy(x => Tree.GetParentOf<DriveM>(x.Folder))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

  public override MediaItemM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2]) {
      Width = csv[3].IntParseOrDefault(0),
      Height = csv[4].IntParseOrDefault(0),
      Orientation = csv[5].IntParseOrDefault(1),
      Rating = csv[6].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[7]) ? null : csv[7],
      IsOnlyInDb = csv[11] == "1"
    };

  public override string ToCsv(MediaItemM mediaItem) =>
    string.Join("|",
      mediaItem.GetHashCode().ToString(),
      mediaItem.Folder.GetHashCode().ToString(),
      mediaItem.FileName,
      mediaItem.Width.ToString(),
      mediaItem.Height.ToString(),
      mediaItem.Orientation.ToString(),
      mediaItem.Rating.ToString(),
      mediaItem.Comment ?? string.Empty,
      mediaItem.GeoName?.GetHashCode().ToString(),
      mediaItem.People == null
        ? string.Empty
        : string.Join(",", mediaItem.People.Select(x => x.GetHashCode().ToString())),
      mediaItem.Keywords == null
        ? string.Empty
        : string.Join(",", mediaItem.Keywords.Select(x => x.GetHashCode().ToString())),
      mediaItem.IsOnlyInDb
        ? "1"
        : string.Empty);

  public override void LinkReferences() {
    foreach (var (mi, csv) in AllCsv) {
      // reference to Folder and back reference from Folder to MediaItems
      mi.Folder = Core.Db.Folders.AllDict[int.Parse(csv[1])];
      mi.Folder.MediaItems.Add(mi);

      // reference to People
      mi.People = LinkList(csv[9], Core.Db.People.AllDict);

      // reference to Keywords
      mi.Keywords = LinkList(csv[10], Core.Db.Keywords.AllDict);

      // reference to GeoName
      if (!string.IsNullOrEmpty(csv[8]))
        mi.GeoName = Core.Db.GeoNames.AllDict[int.Parse(csv[8])];
    }
  }
}