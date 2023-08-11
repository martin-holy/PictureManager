using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsM : ObservableObject {
    private readonly Core _core;
    private double _segmentUiSize;
    private bool _canSelectAsSamePerson;

    public SegmentsDataAdapter DataAdapter { get; set; }
    public SegmentsRectsM SegmentsRectsM { get; }
    public SegmentsDrawerM SegmentsDrawerM { get; }
    public SegmentsView SegmentsView { get; private set; }
    public Selecting<SegmentM> Selected { get; } = new();
    public static int SegmentSize = 100;
    public bool CanSelectAsSamePerson { get => _canSelectAsSamePerson; set { _canSelectAsSamePerson = value; OnPropertyChanged(); } }
    public double SegmentUiFullWidth { get; set; }
    public double SegmentUiSize { get => _segmentUiSize; set { _segmentUiSize = value; OnPropertyChanged(); } }

    public CanDragFunc CanDragFunc { get; }

    public event EventHandler<ObjectEventArgs<(SegmentM, PersonM, PersonM)>> SegmentPersonChangeEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<(SegmentM[], PersonM[])>> SegmentsPersonChangedEvent = delegate { };
    public event EventHandler<ObjectEventArgs<(SegmentM[], KeywordM)>> SegmentsKeywordChangedEvent = delegate { };
    public event EventHandler<ObjectEventArgs<SegmentM>> SegmentDeletedEventHandler = delegate { };

    public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
    public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
    public RelayCommand<SegmentM> ViewMediaItemsWithSegmentCommand { get; }
    public RelayCommand<object> OpenSegmentsViewCommand { get; }

    public SegmentsM(Core core) {
      _core = core;
      SegmentsRectsM = new(this);
      SegmentsDrawerM = new(this, _core);

      SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
      SetSelectedAsUnknownCommand = new(SetSelectedAsUnknown, () => Selected.Items.Count > 0);
      ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
      OpenSegmentsViewCommand = new(
        OpenSegmentsView,
        () => _core.MediaItemsViews.Current?.FilteredItems.Count > 0);

      CanDragFunc = CanDrag;
    }

    public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
      if (!isCtrlOn && !isShiftOn)
        _core.PeopleM.Selected.DeselectAll();

      Selected.Select(segments, segment, isCtrlOn, isShiftOn);
      _core.PeopleM.Selected.Add(Selected.Items
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct());
      SetCanSelectAsSamePerson();
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? GetOneOrSelected(segmentM)
        : null;

    public void SetCanSelectAsSamePerson() {
      CanSelectAsSamePerson =
        Selected.Items.GroupBy(x => x.Person).Count() > 1
        || Selected.Items.Count(x => x.Person == null) > 1;
    }

    public void SetSegmentUiSize(double size, double scrollBarSize) {
      SegmentUiSize = size;
      SegmentUiFullWidth = size + 6; // + border, margin
    }

    public SegmentM[] GetOneOrSelected(SegmentM one) =>
      Selected.Items.Contains(one)
        ? Selected.Items.ToArray()
        : new[] { one };

    public SegmentM AddNewSegment(double x, double y, int size, MediaItemM mediaItem) {
      var newSegment = new SegmentM(DataAdapter.GetNextId(), x, y, size) { MediaItem = mediaItem };
      mediaItem.Segments ??= new();
      mediaItem.Segments.Add(newSegment);
      DataAdapter.All.Add(newSegment);
      SegmentsView?.CvSegments.ReGroupItems(new[] { newSegment }, false);

      return newSegment;
    }

    public SegmentM GetCopy(SegmentM s) =>
      new(DataAdapter.GetNextId(), s.X, s.Y, s.Size) {
        MediaItem = s.MediaItem,
        Person = s.Person,
        Keywords = s.Keywords?.ToList()
      };

    /// <summary>
    /// Sets new Person to all Segments that are selected 
    /// or that have the same Person (with id less than 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    public void SetSelectedAsPerson(PersonM person) {
      var unknownPeople = Selected.Items
        .Where(x => x.Person?.Id < 0)
        .Select(x => x.Person)
        .Distinct()
        .ToHashSet();
      var segments = Selected.Items
        .Where(x => x.Person == null || x.Person.Id > 0)
        .Concat(DataAdapter.All.Where(x => unknownPeople.Contains(x.Person)))
        .ToArray();
      var people = segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Concat(new[] { person })
        .Distinct()
        .ToArray();

      _core.PeopleM.MergePeople(person, unknownPeople.ToArray());

      foreach (var segment in segments)
        ChangePerson(segment, person);

      Selected.DeselectAll();

      SegmentsPersonChangedEvent(this, new((segments, people)));
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected 
    /// or that have the same Person (not null) as some of the selected.
    /// The new Person is the person with the highest Id from the selected 
    /// or the newly created person with the highest unused negative id.
    /// </summary>
    private void SetSelectedAsSamePerson() {
      if (!CanSelectAsSamePerson) return;

      PersonM newPerson;
      SegmentM[] toUpdate;
      var people = Selected.Items
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .OrderBy(x => x.Name)
        .ToArray();

      if (people.Length == 0) {
        // create person with unused min ID
        var id = -1;
        var usedIds = DataAdapter.All
          .Where(x => x.Person?.Id < 0)
          .Select(x => x.Person.Id)
          .Distinct()
          .OrderByDescending(x => x)
          .ToArray();

        if (usedIds.Any())
          for (var i = -1; i > usedIds.Min() - 2; i--) {
            if (usedIds.Contains(i)) continue;
            id = i;
            break;
          }

        newPerson = new(id, $"P {id}");
        _core.PeopleM.DataAdapter.All.Add(newPerson);
        toUpdate = Selected.Items.ToArray();
      }
      else {
        if (people.Length == 1) {
          newPerson = people[0];
          toUpdate = GetSegmentsToUpdate(newPerson, people);
        }
        else {
          var spd = new SetSegmentPersonDialogM(this, people);
          if (Core.DialogHostShow(spd) != 1) return;
          newPerson = spd.Person;
          toUpdate = spd.Segments;
        }

        _core.PeopleM.MergePeople(newPerson, people.Where(x => !x.Equals(newPerson)).ToArray());
      }

      var affectedPeople = people.Concat(new[] { newPerson }).Distinct().ToArray();

      foreach (var segment in toUpdate)
        ChangePerson(segment, newPerson);

      Selected.DeselectAll();
      _core.PeopleM.Selected.DeselectAll();
      SegmentsPersonChangedEvent(this, new((toUpdate, affectedPeople)));
    }

    public SegmentM[] GetSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
      var oldPeople = people.Where(x => !x.Equals(person)).ToHashSet();
      return DataAdapter.All
        .Where(x => oldPeople.Contains(x.Person))
        .Concat(Selected.Items.Where(x => x.Person == null))
        .ToArray();
    }

    private void SetSelectedAsUnknown() {
      var msgCount = Selected.Items.Count == 1
        ? "selected segment"
        : $"{Selected.Items.Count} selected segments";
      var msg = $"Do you want to set {msgCount} as unknown?";

      if (Core.DialogHostShow(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1)
        return;

      var segments = Selected.Items.ToArray();
      var people = segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToArray();
      foreach (var segment in segments)
        ChangePerson(segment, null);

      Selected.DeselectAll();
      SegmentsPersonChangedEvent(this, new((segments, people)));
    }

    private void ToggleKeyword(SegmentM segment, KeywordM keyword) {
      segment.Keywords = KeywordsM.Toggle(segment.Keywords, keyword);
      DataAdapter.IsModified = true;
    }

    private void ToggleKeyword(SegmentM[] segments, KeywordM keyword) {
      foreach (var segment in segments)
        ToggleKeyword(segment, keyword);

      SegmentsKeywordChangedEvent(this, new((segments, keyword)));
    }

    public void RemoveKeywordFromSegments(KeywordM keyword) =>
      ToggleKeyword(DataAdapter.All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

    public void ToggleKeywordOnSelected(KeywordM keyword) =>
      ToggleKeyword(Selected.Items.ToArray(), keyword);

    public void RemovePersonFromSegments(PersonM person) {
      foreach (var segment in DataAdapter.All.Where(s => s.Person?.Equals(person) == true)) {
        segment.Person = null;
        DataAdapter.IsModified = true;
      }
    }

    private void ChangePerson(SegmentM segment, PersonM person) {
      SegmentPersonChangeEventHandler(this, new((segment, segment.Person, person)));
      segment.Person = person;
      segment.MediaItem.SetInfoBox();
      DataAdapter.IsModified = true;
    }

    public void Delete(IEnumerable<SegmentM> segments) {
      if (segments == null) return;
      foreach (var segment in segments.ToArray())
        Delete(segment);
    }

    public void Delete(SegmentM segment) {
      DataAdapter.All.Remove(segment);
      SegmentDeletedEventHandler(this, new(segment));
      Selected.Set(segment, false);
      ChangePerson(segment, null);

      // remove Segment from MediaItem
      if (segment.MediaItem.Segments.Remove(segment) && !segment.MediaItem.Segments.Any())
        segment.MediaItem.Segments = null;

      try {
        if (File.Exists(segment.FilePathCache))
          File.Delete(segment.FilePathCache);

        segment.MediaItem = null;
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public List<MediaItemM> GetMediaItemsWithSegment(SegmentM segmentM) {
      if (segmentM.MediaItem == null) return null;

      if (ReferenceEquals(SegmentsView?.CvSegments.LastSelectedItem, segmentM))
        return SegmentsView.CvSegments.LastSelectedRow.Parent.Source
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.Folder.FullPath)
          .ThenBy(x => x.FileName)
          .ToList();

      if (segmentM.Person != null)
        return DataAdapter.All
          .Where(x => x.Person == segmentM.Person)
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.FileName)
          .ToList();

      if (SegmentsDrawerM.Items.Contains(segmentM))
        return SegmentsDrawerM.Items
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.Folder.FullPath)
          .ThenBy(x => x.FileName)
          .ToList();

      return new() { segmentM.MediaItem };
    }

    private SegmentM[] GetSegments(List<MediaItemM> mediaItems, int mode) {
      switch (mode) {
        case 1: // all segments from mediaItems
          return mediaItems
            .Where(x => x.Segments != null)
            .SelectMany(x => x.Segments)
            .ToArray();
        case 2: // all segments with person found on segments from mediaItems
          var people = mediaItems
            .Where(mi => mi.Segments != null)
            .SelectMany(mi => mi.Segments
              .Where(x => x.Person != null)
              .Select(x => x.Person))
            .Distinct()
            .ToHashSet();

          return DataAdapter.All
            .Where(x => x.Person != null && people.Contains(x.Person))
            .OrderBy(x => x.MediaItem.FileName)
            .ToArray();
        case 3: // one segment from each person
          return DataAdapter.All
            .Where(x => x.Person != null)
            .GroupBy(x => x.Person.Id)
            .Select(x => x.First())
            .ToArray();
        default:
          return Array.Empty<SegmentM>();
      }
    }

    private void ViewMediaItemsWithSegment(SegmentM segmentM) {
      var items = GetMediaItemsWithSegment(segmentM);
      if (items == null) return;

      _core.MediaViewerM.SetMediaItems(items, segmentM.MediaItem);
      _core.MainWindowM.IsFullScreen = true;
    }

    private static int GetSegmentsToLoadUserInput() {
      var md = new MessageDialog(
        "Segments",
        "Do you want to load all segments, segments with persons \nor one segment from each person?",
        Res.IconQuestion,
        true);

      md.Buttons = new DialogButton[] {
        new("All segments", null, md.SetResult(1), true),
        new("Segments with persons", null, md.SetResult(2)),
        new("One from each", null, md.SetResult(3)) };

      return Core.DialogHostShow(md);
    }

    private void OpenSegmentsView() {
      var result = GetSegmentsToLoadUserInput();
      if (result < 1) return;

      var segments = GetSegments(_core.MediaItemsViews.Current.GetSelectedOrAll(), result).ToList();
      SegmentsView ??= new(_core.PeopleM, this);
      _core.MainTabsM.Activate(SegmentsView, "Segments");
      SegmentsView.Reload(segments);
    }
  }
}