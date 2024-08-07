﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Features.Segment;

/// <summary>
/// DB fields: ID|MediaItemId|PersonId|SegmentBox|Keywords
/// </summary>
public class SegmentR : TableDataAdapter<SegmentM> {
  private readonly CoreR _coreR;
  private List<int> _drawerNotAvailable = [];

  public List<SegmentM> Drawer { get; private set; } = [];
  public event EventHandler<(SegmentM, PersonM?, PersonM?)> SegmentPersonChangedEvent = delegate { };
  public event EventHandler<(SegmentM[], PersonM?, PersonM[])> SegmentsPersonChangedEvent = delegate { };
  public event EventHandler<SegmentM[]> SegmentsKeywordsChangedEvent = delegate { };

  public SegmentR(CoreR coreR) : base(coreR, "Segments", 5) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<SegmentM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.MediaItem.Folder);

  public override SegmentM FromCsv(string[] csv) {
    var rect = csv[3].Split(',');
    return new(int.Parse(csv[0]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]), MediaItemR.Dummy);
  }

  public override string ToCsv(SegmentM segment) =>
    string.Join("|",
      segment.GetHashCode().ToString(),
      segment.MediaItem.GetHashCode().ToString(),
      segment.Person == null
        ? "0"
        : segment.Person.GetHashCode().ToString(),
      string.Join(",", ((int)segment.X).ToString(), ((int)segment.Y).ToString(), ((int)segment.Size).ToString()),
      segment.Keywords.ToHashCodes().ToCsv());

  public override void PropsToCsv() {
    TableProps.Clear();
    TableProps.Add(nameof(SegmentVM.SegmentSize), SegmentVM.SegmentSize.ToString());
    TableProps.Add("SegmentsDrawer", string.Join(",",
      Drawer
        .Select(x => x.GetHashCode())
        .Concat(_drawerNotAvailable)
        .Select(x => x.ToString())));
  }

  public override void LinkReferences() {
    var withoutMediaItem = new List<SegmentM>();

    foreach (var (segment, csv) in AllCsv) {
      var mi = _coreR.MediaItem.GetById(csv[1]);
      if (mi != null) {
        segment.MediaItem = mi;
        mi.Segments ??= [];
        mi.Segments.Add(segment);

        var personId = int.Parse(csv[2]);

        if (personId != 0) {
          segment.Person = _coreR.Person.GetPerson(personId, this);
          segment.Person.Segment ??= segment;
          segment.Person.Segments ??= [];
          segment.Person.Segments.Add(segment);
        }
      }
      else
        withoutMediaItem.Add(segment);

      // reference to Keywords
      segment.Keywords = _coreR.Keyword.Link(csv[4], this);
    }

    // in case MediaItem was deleted
    foreach (var segment in withoutMediaItem)
      _ = AllDict.Remove(segment.GetHashCode());

    // Table Properties
    if (TableProps.TryGetValue(nameof(SegmentVM.SegmentSize), out var segmentSize))
      SegmentVM.SegmentSize = int.Parse(segmentSize);

    if (TableProps.TryGetValue("SegmentsDrawer", out var segmentsDrawer)
        && !string.IsNullOrEmpty(segmentsDrawer)
        && IdsToRecords(segmentsDrawer, AllDict) is { } drawer) {
        Drawer = drawer.Item1;
        _drawerNotAvailable = drawer.Item2;
    }

    // table props are not needed any more
    TableProps.Clear();
  }

  public List<SegmentM>? Link(string csv, IDataAdapter seeker) =>
    LinkList(csv, null, seeker);

  public SegmentM ItemCreate(double x, double y, int size, MediaItemM mediaItem) =>
    ItemCreate(new(GetNextId(), x, y, size, mediaItem));

  public SegmentM ItemCopy(SegmentM item, MediaItemM mediaItem) =>
    ItemCreate(new(GetNextId(), item.X, item.Y, item.Size, mediaItem) {
      Person = item.Person,
      Keywords = item.Keywords?.ToList()
    });

  protected override void OnItemDeleted(object sender, SegmentM item) {
    File.Delete(item.FilePathCache);
  }

  public IEnumerable<SegmentM> GetBy(KeywordM keyword, bool recursive) =>
    All.GetBy(keyword, recursive);

  public IEnumerable<SegmentM> GetBy(PersonM person) =>
    All.Where(x => ReferenceEquals(x.Person, person));

  public void RemovePerson(PersonM person) {
    var segments = All.Where(x => ReferenceEquals(x.Person, person)).ToArray();
    if (segments.Length == 0) return;
    foreach (var segment in segments) {
      segment.Person = null;
      IsModified = true;
    }

    SegmentsPersonChangedEvent(this, (segments, null, [person]));
  }

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(SegmentM[] segments, KeywordM keyword) =>
    keyword.Toggle(segments, _ => IsModified = true, () => SegmentsKeywordsChangedEvent(this, segments));

  public void ChangePerson(PersonM? person, SegmentM[] segments, PersonM[] people) {
    foreach (var segment in segments)
      ChangePerson(segment, person);

    SegmentsPersonChangedEvent(this, (segments, person, people));
  }

  private void ChangePerson(SegmentM segment, PersonM? person) {
    var oldPerson = segment.Person;
    segment.Person = person;
    IsModified = true;
    SegmentPersonChangedEvent(this, (segment, oldPerson, person));
  }
}