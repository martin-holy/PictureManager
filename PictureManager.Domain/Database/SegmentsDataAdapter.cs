using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|MediaItemId|PersonId|SegmentBox|Keywords
/// </summary>
public class SegmentsDataAdapter : DataAdapter<SegmentM> {
  private readonly Db _db;

  public SegmentsM Model { get; }
  public event EventHandler<ObjectEventArgs<(SegmentM, PersonM, PersonM)>> SegmentPersonChangedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<(PersonM, SegmentM[], PersonM[])>> SegmentsPersonChangedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<SegmentM[]>> SegmentsKeywordsChangedEvent = delegate { };

  public SegmentsDataAdapter(Db db) : base("Segments", 5) {
    _db = db;
    Model = new(this);
  }

  public override void Save() =>
    SaveDriveRelated(All
      .GroupBy(x => Tree.GetParentOf<DriveM>(x.MediaItem.Folder))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

  public override SegmentM FromCsv(string[] csv) {
    var rect = csv[3].Split(',');
    return new(int.Parse(csv[0]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]));
  }

  public override string ToCsv(SegmentM segment) =>
    string.Join("|",
      segment.GetHashCode().ToString(),
      segment.MediaItem.GetHashCode().ToString(),
      segment.Person == null
        ? "0"
        : segment.Person.GetHashCode().ToString(),
      string.Join(",", ((int)segment.X).ToString(), ((int)segment.Y).ToString(), ((int)segment.Size).ToString()),
      segment.Keywords == null
        ? string.Empty
        : string.Join(",", segment.Keywords.Select(x => x.GetHashCode().ToString())));

  public override void PropsToCsv() {
    TableProps.Clear();
    TableProps.Add(nameof(SegmentsM.SegmentSize), SegmentsM.SegmentSize.ToString());
    TableProps.Add("SegmentsDrawer", string.Join(",", Model.SegmentsDrawerM.Items.Select(x => x.GetHashCode().ToString())));
  }

  public override void LinkReferences() {
    var withoutMediaItem = new List<SegmentM>();

    foreach (var (segment, csv) in AllCsv) {
      if (_db.MediaItems.AllDict.TryGetValue(int.Parse(csv[1]), out var mi)) {
        segment.MediaItem = mi;
        mi.Segments ??= new();
        mi.Segments.Add(segment);

        var personId = int.Parse(csv[2]);

        if (personId != 0) {
          if (!_db.People.AllDict.TryGetValue(personId, out var person)) {
            // this needs to stay because not all segments have to be loaded
            // (segments from other drives)
            person = new(personId, $"P {personId}");
            _db.People.AllDict.Add(person.GetHashCode(), person);
          }

          segment.Person = person;
          person.Segment ??= segment;
        }
      }
      else {
        withoutMediaItem.Add(segment);
      }

      // reference to Keywords
      segment.Keywords = LinkList(csv[4], _db.Keywords.AllDict);
    }

    // in case MediaItem was deleted
    foreach (var segment in withoutMediaItem)
      _ = AllDict.Remove(segment.GetHashCode());

    // Table Properties
    if (TableProps == null) return;
    if (TableProps.TryGetValue(nameof(SegmentsM.SegmentSize), out var segmentSize))
      SegmentsM.SegmentSize = int.Parse(segmentSize);
    if (TableProps.TryGetValue("SegmentsDrawer", out var segmentsDrawer) && !string.IsNullOrEmpty(segmentsDrawer)) {
      Model.SegmentsDrawerM.Items.Clear();

      var drawer = segmentsDrawer
        .Split(',')
        .Select(id => AllDict[int.Parse(id)])
        .OrderBy(x => x.MediaItem.Folder.FullPath)
        .ThenBy(x => x.MediaItem.FileName);

      foreach (var segment in drawer)
        Model.SegmentsDrawerM.Items.Add(segment);
    }

    // table props are not needed any more
    TableProps.Clear();
  }

  public SegmentM ItemCreate(double x, double y, int size, MediaItemM mediaItem) =>
    ItemCreate(new(GetNextId(), x, y, size) { MediaItem = mediaItem });

  public SegmentM ItemCopy(SegmentM item, MediaItemM mediaItem) =>
    ItemCreate(new(GetNextId(), item.X, item.Y, item.Size) {
      MediaItem = mediaItem,
      Person = item.Person,
      Keywords = item.Keywords?.ToList()
    });

  private SegmentM ItemCreate(SegmentM item) {
    All.Add(item);
    RaiseItemCreated(item);
    OnItemCreated(item);

    return item;
  }

  protected override void OnItemCreated(SegmentM item) {
    item.MediaItem.Segments ??= new();
    item.MediaItem.Segments.Add(item);
  }

  public void ItemsDelete(IList<SegmentM> items) {
    if (items == null) return;
    foreach (var item in items) ItemDelete(item);
    RaiseItemsDeleted(items);
  }

  public void ItemDelete(SegmentM item) {
    All.Remove(item);
    RaiseItemDeleted(item);
    OnItemDeleted(item);
  }

  protected override void OnItemDeleted(SegmentM item) {
    if (item.MediaItem.Segments.Remove(item) && !item.MediaItem.Segments.Any())
      item.MediaItem.Segments = null;

    item.MediaItem = null;
    item.Person = null;
  }

  public void RemovePersonFromSegments(PersonM person) {
    foreach (var segment in All.Where(s => s.Person?.Equals(person) == true)) {
      segment.Person = null;
      IsModified = true;
    }
  }

  public void RemoveKeywordFromSegments(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(SegmentM[] segments, KeywordM keyword) {
    foreach (var segment in segments) {
      segment.Keywords = KeywordsM.Toggle(segment.Keywords, keyword);
      IsModified = true;
    }

    SegmentsKeywordsChangedEvent(this, new(segments));
  }

  public void ChangePerson(PersonM person, SegmentM[] segments, PersonM[] people) {
    foreach (var segment in segments)
      ChangePerson(segment, person);

    SegmentsPersonChangedEvent(this, new((person, segments, people)));
  }

  private void ChangePerson(SegmentM segment, PersonM person) {
    var oldPerson = segment.Person;
    segment.Person = person;
    IsModified = true;
    SegmentPersonChangedEvent(this, new((segment, oldPerson, person)));
  }
}