using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models;

public sealed class SegmentsM : ObservableObject {
  private bool _canSetAsSamePerson;

  public SegmentsDataAdapter DataAdapter { get; set; }
  public SegmentsRectsM SegmentsRectsM { get; }
  public SegmentsDrawerM SegmentsDrawerM { get; }
  public SegmentsView SegmentsView { get; private set; }
  public Selecting<SegmentM> Selected { get; } = new();
  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }
  public bool CanSetAsSamePerson { get => _canSetAsSamePerson; set { _canSetAsSamePerson = value; OnPropertyChanged(); } }
    
  public CanDragFunc CanDragFunc { get; }

  public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
  public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
  public RelayCommand<SegmentM> ViewMediaItemsWithSegmentCommand { get; }
  public RelayCommand<object> OpenSegmentsViewCommand { get; }

  public SegmentsM(SegmentsDataAdapter da) {
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    DataAdapter.ItemDeletedEvent += OnItemDeleted;
    DataAdapter.SegmentsKeywordsChangedEvent += OnSegmentsKeywordsChanged;
    DataAdapter.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;

    SegmentsRectsM = new(this);
    SegmentsDrawerM = new(this);

    SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
    SetSelectedAsUnknownCommand = new(
      () => SetAsUnknown(Selected.Items.ToArray()),
      () => Selected.Items.Count > 0);
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
    SegmentsDrawerM.Remove(e.Data);
    SegmentsView?.CvSegments.ReGroupItems(new[] { e.Data }, true);

    try {
      if (File.Exists(e.Data.FilePathCache))
        File.Delete(e.Data.FilePathCache);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  private void OnSegmentsKeywordsChanged(object sender, ObjectEventArgs<SegmentM[]> e) {
    SegmentsView?.CvSegments.ReGroupItems(e.Data, false);
  }
  
  private void OnSegmentsPersonChanged(object sender, ObjectEventArgs<(PersonM, SegmentM[], PersonM[])> e) {
    SegmentsView?.CvSegments.ReGroupItems(e.Data.Item2, false);
    SegmentsView?.CvPeople.ReGroupItems(e.Data.Item3?.Where(x => x.Segment != null).ToArray(), false);
    Selected.DeselectAll();
  }

  public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
    if (!isCtrlOn && !isShiftOn)
      Core.PeopleM.Selected.DeselectAll();

    Selected.Select(segments, segment, isCtrlOn, isShiftOn);
    Core.PeopleM.Selected.Add(Selected.Items.GetPeople());
    SetCanSetAsSamePerson();
  }

  private object CanDrag(object source) =>
    source is SegmentM segmentM
      ? GetOneOrSelected(segmentM)
      : null;

  public void SetCanSetAsSamePerson() {
    CanSetAsSamePerson =
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
  public void SetSelectedAsPerson(PersonM person) {
    var unknownPeople = Selected.Items.GetPeople().Where(x => x.IsUnknown).ToHashSet();
    var segments = Selected.Items
      .Where(x => x.Person == null || !x.Person.IsUnknown)
      .Concat(DataAdapter.All.Where(x => unknownPeople.Contains(x.Person)))
      .ToArray();
    var people = segments
      .GetPeople()
      .Concat(new[] { person })
      .Distinct()
      .ToArray();

    Core.PeopleM.MergePeople(person, unknownPeople.ToArray());
    DataAdapter.ChangePerson(person, segments, people);
  }

  private void SetSelectedAsSamePerson() {
    if (!CanSetAsSamePerson) return;

    PersonM newPerson;
    SegmentM[] toUpdate;
    var people = Selected.Items.GetPeople().OrderBy(x => x.Name).ToArray();

    if (people.Length == 0) {
      newPerson = Core.Db.People.ItemCreateUnknown();
      toUpdate = Selected.Items.ToArray();
    }
    else if (people.Length == 1) {
      newPerson = people[0];
      toUpdate = Selected.Items.Where(x => x.Person == null).ToArray();
    }
    else {
      var spd = new SetSegmentPersonDialogM(this, people);
      if (Dialog.Show(spd) != 1) return;
      newPerson = spd.Person;
      toUpdate = spd.Segments;
      Core.PeopleM.MergePeople(newPerson, people.Where(x => !x.Equals(newPerson)).ToArray());
    }

    Core.PeopleM.Selected.DeselectAll();
    var affectedPeople = people.Concat(new[] { newPerson }).Distinct().ToArray();
    DataAdapter.ChangePerson(newPerson, toUpdate, affectedPeople);
  }

  private void SetAsUnknown(SegmentM[] segments) {
    var msgCount = segments.Length == 1
      ? "selected segment"
      : $"{segments.Length} selected segments";
    var msg = $"Do you want to set {msgCount} as unknown?";

    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1)
      return;

    var people = segments.GetPeople().ToArray();
    DataAdapter.ChangePerson(null, segments, people);
  }

  private void ViewMediaItemsWithSegment(SegmentM segmentM) {
    var items = GetMediaItemsWithSegment(segmentM);
    if (items == null) return;

    Core.MediaViewerM.SetMediaItems(items, segmentM.MediaItem);
    Core.MainWindowM.IsFullScreen = true;
  }

  private List<MediaItemM> GetMediaItemsWithSegment(SegmentM segment) {
    if (segment == null) return null;

    if (ReferenceEquals(SegmentsView?.CvSegments.LastSelectedItem, segment))
      return ((CollectionViewGroup<SegmentM>)SegmentsView.CvSegments.LastSelectedRow.Parent).Source
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName)
        .ToList();

    if (segment.Person != null)
      return DataAdapter.All.Where(x => x.Person == segment.Person)
        .GetMediaItems()
        .OrderBy(x => x.FileName)
        .ToList();

    if (SegmentsDrawerM.Items.Contains(segment))
      return SegmentsDrawerM.Items
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName)
        .ToList();

    return new() { segment.MediaItem };
  }

  private void OpenSegmentsView() {
    var result = GetSegmentsToLoadUserInput();
    if (result < 1) return;

    var segments = GetSegments(Core.MediaItemsViews.Current.GetSelectedOrAll(), result).ToArray();
    SegmentsView ??= new(Core.PeopleM, this);
    Core.MainTabs.Activate(Res.IconSegment, "Segments", SegmentsView);
    SegmentsView.Reload(segments);
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

  private IEnumerable<SegmentM> GetSegments(IEnumerable<MediaItemM> mediaItems, int mode) {
    switch (mode) {
      case 1: // all segments from mediaItems
        return mediaItems.GetSegments();
      case 2: // all segments with person found on segments from mediaItems
        var people = mediaItems.GetSegments().GetPeople().ToHashSet();

        return DataAdapter.All
          .Where(x => x.Person != null && people.Contains(x.Person))
          .OrderBy(x => x.MediaItem.FileName);
      case 3: // one segment from each person
        return DataAdapter.All
          .Where(x => x.Person != null)
          .GroupBy(x => x.Person.Id)
          .Select(x => x.First());
      default:
        return Enumerable.Empty<SegmentM>();
    }
  }
}