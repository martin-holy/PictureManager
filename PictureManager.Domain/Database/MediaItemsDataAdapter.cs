using System.Collections.Generic;
using System.IO;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Linq;
using System;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
/// </summary>
public sealed class MediaItemsDataAdapter : DataAdapter<MediaItemM> {
  public event EventHandler<ObjectEventArgs<MediaItemM>> ItemRenamedEvent = delegate { };

  public MediaItemsDataAdapter() : base("MediaItems", 12) { }

  private void RaiseItemRenamed(MediaItemM item) => ItemRenamedEvent(this, new(item));

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

  public IEnumerable<MediaItemM> GetItems(RatingM rating) =>
    All.Where(x => x.Rating == rating.Value);

  public IEnumerable<MediaItemM> GetItems(PersonM person) =>
    All.Where(mi =>
        mi.People?.Contains(person) == true ||
        mi.Segments?.Any(s => s.Person == person) == true)
      .OrderBy(mi => mi.FileName);

  public IEnumerable<MediaItemM> GetItems(KeywordM keyword, bool recursive) {
    var keywords = new List<KeywordM> { keyword };
    if (recursive) Tree.GetThisAndItemsRecursive(keyword, ref keywords);
    var set = new HashSet<KeywordM>(keywords);

    return All
      .Where(mi => mi.Keywords?.Any(k => set.Contains(k)) == true
                   || mi.Segments?.Any(s => s.Keywords?.Any(k => set.Contains(k)) == true) == true);
  }

  public IEnumerable<MediaItemM> GetItems(GeoNameM geoName, bool recursive) {
    var geoNames = new List<GeoNameM> { geoName };
    if (recursive) Tree.GetThisAndItemsRecursive(geoName, ref geoNames);
    var set = new HashSet<GeoNameM>(geoNames);

    return All.Where(mi => set.Contains(mi.GeoName))
      .OrderBy(x => x.FileName);
  }

  public MediaItemM ItemCreate(FolderM folder, string fileName) {
    var item = new MediaItemM(GetNextId(), folder, fileName);
    folder.MediaItems.Add(item);
    All.Add(item);
    RaiseItemCreated(item);

    return item;
  }

  public void ItemMove(MediaItemM item, FolderM folder, string fileName) {
    item.FileName = fileName;
    item.Folder.MediaItems.Remove(item);
    item.Folder = folder;
    item.Folder.MediaItems.Add(item);

    IsModified = true;
  }

  public MediaItemM ItemCopy(MediaItemM item, FolderM folder, string fileName) {
    var copy = new MediaItemM(GetNextId(), folder, fileName) {
      Width = item.Width,
      Height = item.Height,
      Orientation = item.Orientation,
      Rating = item.Rating,
      Comment = item.Comment,
      GeoName = item.GeoName,
      Lat = item.Lat,
      Lng = item.Lng
    };

    if (item.People != null)
      copy.People = new(item.People);

    if (item.Keywords != null)
      copy.Keywords = new (item.Keywords);

    if (item.Segments != null) {
      copy.Segments = new();
      foreach (var segment in item.Segments) {
        var sCopy = Core.SegmentsM.GetCopy(segment);
        sCopy.MediaItem = copy;
        copy.Segments.Add(sCopy);
      }
    }

    copy.Folder.MediaItems.Add(copy);
    All.Add(copy);
    RaiseItemCreated(copy);

    return copy;
  }

  public void ItemDelete(MediaItemM item) {
    if (item == null) return;
    All.Remove(item);
    IsModified = true;
    RaiseItemDeleted(item);
    OnItemDeleted(item);
  }

  public void ItemsDelete(IList<MediaItemM> items) {
    if (items == null || items.Count == 0) return;
    foreach (var mi in items) ItemDelete(mi);
    RaiseItemsDeleted(items);
  }

  protected override void OnItemDeleted(MediaItemM item) {
    item.People = null;
    item.Keywords = null;
    item.GeoName = null;
    item.Folder.MediaItems.Remove(item);
    // TODO test this why is commented out
    //item.Folder = null;
  }

  public void ItemRename(MediaItemM item, string newFileName) {
    var oldFilePath = item.FilePath;
    var oldFilePathCache = item.FilePathCache;
    item.FileName = newFileName;
    File.Move(oldFilePath, item.FilePath);
    File.Move(oldFilePathCache, item.FilePathCache);
    IsModified = true;
    RaiseItemRenamed(item);
  }
}