using MH.Utils.BaseClasses;
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
  public event EventHandler<(SegmentM, PersonM?, PersonM?)>? SegmentPersonChangedEvent;
  public event EventHandler<(SegmentM[], PersonM?, PersonM[])>? SegmentsPersonChangedEvent;
  public event EventHandler<SegmentM[]>? SegmentsKeywordsChangedEvent;

  public SegmentR(CoreR coreR) : base(coreR, "Segments", 5) {
    _coreR = coreR;
    IsDriveRelated = true;
  }

  protected override Dictionary<string, IEnumerable<SegmentM>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x.MediaItem.Folder);

  protected override SegmentM _fromCsv(string[] csv) {
    var rect = csv[3].Split(',');
    return new(int.Parse(csv[0]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]), MediaItemR.Dummy);
  }

  protected override string _toCsv(SegmentM segment) =>
    string.Join("|",
      segment.GetHashCode().ToString(),
      segment.MediaItem.GetHashCode().ToString(),
      segment.Person == null
        ? "0"
        : segment.Person.GetHashCode().ToString(),
      string.Join(",", ((int)segment.X).ToString(), ((int)segment.Y).ToString(), ((int)segment.Size).ToString()),
      segment.Keywords.ToHashCodes().ToCsv());

  protected override void _propsToCsv() {
    _tableProps.Clear();
    _tableProps.Add(nameof(SegmentVM.SegmentSize), SegmentVM.SegmentSize.ToString());
    _tableProps.Add("SegmentsDrawer", string.Join(",",
      Drawer
        .Select(x => x.GetHashCode())
        .Concat(_drawerNotAvailable)
        .Select(x => x.ToString())));
  }

  public override void LinkReferences() {
    var withoutMediaItem = new List<SegmentM>();

    foreach (var (segment, csv) in _allCsv) {
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
    if (_tableProps.TryGetValue(nameof(SegmentVM.SegmentSize), out var segmentSize))
      SegmentVM.SegmentSize = int.Parse(segmentSize);

    if (_tableProps.TryGetValue("SegmentsDrawer", out var segmentsDrawer)
        && !string.IsNullOrEmpty(segmentsDrawer)
        && IdsToRecords(segmentsDrawer, AllDict) is { } drawer) {
        Drawer = drawer.Item1;
        _drawerNotAvailable = drawer.Item2;
    }

    // table props are not needed any more
    _tableProps.Clear();
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

  protected override void _onItemDeleted(object sender, SegmentM item) {
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

    _raiseSegmentsPersonChanged((segments, null, [person]));
  }

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(SegmentM[] segments, KeywordM keyword) =>
    keyword.Toggle(segments, _ => IsModified = true, () => _raiseSegmentsKeywordsChanged(segments));

  public void ChangePerson(PersonM? person, SegmentM[] segments, PersonM[] people) {
    foreach (var segment in segments)
      _changePerson(segment, person);

    _raiseSegmentsPersonChanged((segments, person, people));
  }

  private void _changePerson(SegmentM segment, PersonM? person) {
    var oldPerson = segment.Person;
    segment.Person = person;
    IsModified = true;
    _raiseSegmentPersonChanged((segment, oldPerson, person));
  }

  private void _raiseSegmentPersonChanged((SegmentM, PersonM?, PersonM?) args) =>
    SegmentPersonChangedEvent?.Invoke(this, args);

  private void _raiseSegmentsPersonChanged((SegmentM[], PersonM?, PersonM[]) args) =>
    SegmentsPersonChangedEvent?.Invoke(this, args);

  private void _raiseSegmentsKeywordsChanged(SegmentM[] args) =>
    SegmentsKeywordsChangedEvent?.Invoke(this, args);
}