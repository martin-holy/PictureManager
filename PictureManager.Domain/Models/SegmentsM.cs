using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
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
  public Selecting<SegmentM> Selected { get; } = new();
  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }
  public bool CanSetAsSamePerson { get => _canSetAsSamePerson; set { _canSetAsSamePerson = value; OnPropertyChanged(); } }
    
  public CanDragFunc CanDragFunc { get; }

  public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
  public RelayCommand<object> SetSelectedAsUnknownCommand { get; }

  public SegmentsM(SegmentsDataAdapter da) {
    DataAdapter = da;
    DataAdapter.ItemDeletedEvent += OnItemDeleted;
    DataAdapter.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;

    SegmentsRectsM = new(this);
    SegmentsDrawerM = new(this);
    AddEvents(SegmentsDrawerM);

    SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
    SetSelectedAsUnknownCommand = new(
      () => SetAsUnknown(Selected.Items.ToArray()),
      () => Selected.Items.Count > 0);

    CanDragFunc = CanDrag;
  }

  private void OnItemDeleted(object sender, ObjectEventArgs<SegmentM> e) {
    Selected.Set(e.Data, false);
    SegmentsDrawerM.Remove(e.Data);

    try {
      if (File.Exists(e.Data.FilePathCache))
        File.Delete(e.Data.FilePathCache);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  private void OnSegmentsPersonChanged(object sender, ObjectEventArgs<(PersonM, SegmentM[], PersonM[])> e) {
    Selected.DeselectAll();
  }

  public void AddEvents(CollectionViewSegments cv) {
    cv.ItemOpenedEvent += (_, e) => ViewMediaItemsWithSegment(e.Data);
    cv.ItemSelectedEvent += (_, e) => Select(e);
  }

  public void Select(SelectionEventArgs<SegmentM> e) =>
    Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

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
      if (!MergePeopleDialogM.Open(Core.PeopleM, this, people)) return;
      newPerson = MergePeopleDialogM.Person;
      toUpdate = MergePeopleDialogM.SegmentsToUpdate;
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

  public void ViewMediaItemsWithSegment(SegmentM segmentM) {
    var items = GetMediaItemsWithSegment(segmentM);
    if (items == null) return;

    Core.MediaViewerM.SetMediaItems(items, segmentM.MediaItem);
    Core.MainWindowM.IsFullScreen = true;
  }

  private List<MediaItemM> GetMediaItemsWithSegment(SegmentM segment) {
    if (segment == null) return null;

    if (SegmentsView.IsInst && ReferenceEquals(SegmentsView.Inst.CvSegments.LastSelectedItem, segment))
      return ((CollectionViewGroup<SegmentM>)SegmentsView.Inst.CvSegments.LastSelectedRow.Parent).Source
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
}