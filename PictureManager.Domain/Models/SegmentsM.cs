using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models;

public sealed class SegmentsM : ObservableObject {
  private bool _canSelectAsSamePerson;

  public SegmentsDataAdapter DataAdapter { get; set; }
  public SegmentsRectsM SegmentsRectsM { get; }
  public SegmentsDrawerM SegmentsDrawerM { get; }
  public SegmentsView SegmentsView { get; private set; }
  public Selecting<SegmentM> Selected { get; } = new();
  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }
  public bool CanSelectAsSamePerson { get => _canSelectAsSamePerson; set { _canSelectAsSamePerson = value; OnPropertyChanged(); } }
    
  public CanDragFunc CanDragFunc { get; }

  public event EventHandler<ObjectEventArgs<(SegmentM[], PersonM[])>> SegmentsPersonChangedEvent = delegate { };

  public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
  public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
  public RelayCommand<SegmentM> ViewMediaItemsWithSegmentCommand { get; }
  public RelayCommand<object> OpenSegmentsViewCommand { get; }

  public SegmentsM(SegmentsDataAdapter da) {
    DataAdapter = da;
    SegmentsRectsM = new(this);
    SegmentsDrawerM = new(this);

    DataAdapter.ItemCreatedEvent += OnItemCreated;
    DataAdapter.ItemDeletedEvent += OnItemDeleted;

    SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
    SetSelectedAsUnknownCommand = new(SetSelectedAsUnknown, () => Selected.Items.Count > 0);
    ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
    OpenSegmentsViewCommand = new(
      OpenSegmentsView,
      () => Core.MediaItemsViews.Current?.FilteredItems.Count > 0);

    CanDragFunc = CanDrag;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<SegmentM> e) =>
    SegmentsView?.CvSegments.ReGroupItems(new[] { e.Data }, false);

  private void OnItemDeleted(object sender, ObjectEventArgs<SegmentM> e) {
    Selected.Set(e.Data, false);

    try {
      if (File.Exists(e.Data.FilePathCache))
        File.Delete(e.Data.FilePathCache);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
    if (!isCtrlOn && !isShiftOn)
      Core.PeopleM.Selected.DeselectAll();

    Selected.Select(segments, segment, isCtrlOn, isShiftOn);
    Core.PeopleM.Selected.Add(Selected.Items
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

  public static void SetSegmentUiSize(double scale) {
    var size = (int)(SegmentSize / scale);
    SegmentUiSize = size;
    SegmentUiFullWidth = size + 6; // + border, margin
  }

  public SegmentM[] GetOneOrSelected(SegmentM one) =>
    Selected.Items.Contains(one)
      ? Selected.Items.ToArray()
      : new[] { one };

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

    Core.PeopleM.MergePeople(person, unknownPeople.ToArray());

    foreach (var segment in segments)
      DataAdapter.ChangePerson(segment, person);

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

      newPerson = Core.Db.People.ItemCreateUnknown(id);
      toUpdate = Selected.Items.ToArray();
    }
    else {
      if (people.Length == 1) {
        newPerson = people[0];
        toUpdate = GetSegmentsToUpdate(newPerson, people);
      }
      else {
        var spd = new SetSegmentPersonDialogM(this, people);
        if (Dialog.Show(spd) != 1) return;
        newPerson = spd.Person;
        toUpdate = spd.Segments;
      }

      Core.PeopleM.MergePeople(newPerson, people.Where(x => !x.Equals(newPerson)).ToArray());
    }

    var affectedPeople = people.Concat(new[] { newPerson }).Distinct().ToArray();

    foreach (var segment in toUpdate)
      DataAdapter.ChangePerson(segment, newPerson);

    Selected.DeselectAll();
    Core.PeopleM.Selected.DeselectAll();
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

    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1)
      return;

    var segments = Selected.Items.ToArray();
    var people = segments
      .Where(x => x.Person != null)
      .Select(x => x.Person)
      .Distinct()
      .ToArray();
    foreach (var segment in segments)
      DataAdapter.ChangePerson(segment, null);

    Selected.DeselectAll();
    SegmentsPersonChangedEvent(this, new((segments, people)));
  }

  public List<MediaItemM> GetMediaItemsWithSegment(SegmentM segmentM) {
    if (segmentM.MediaItem == null) return null;

    if (ReferenceEquals(SegmentsView?.CvSegments.LastSelectedItem, segmentM))
      return ((CollectionViewGroup<SegmentM>)SegmentsView.CvSegments.LastSelectedRow.Parent).Source
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

    Core.MediaViewerM.SetMediaItems(items, segmentM.MediaItem);
    Core.MainWindowM.IsFullScreen = true;
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

    return Dialog.Show(md);
  }

  private void OpenSegmentsView() {
    var result = GetSegmentsToLoadUserInput();
    if (result < 1) return;

    var segments = GetSegments(Core.MediaItemsViews.Current.GetSelectedOrAll(), result).ToList();
    SegmentsView ??= new(Core.PeopleM, this);
    Core.MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    SegmentsView.Reload(segments);
  }
}