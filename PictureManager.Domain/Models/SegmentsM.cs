using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class SegmentsM : ObservableObject {
  public SegmentsDA DataAdapter { get; set; }
  public SegmentsRectsM SegmentsRectsM { get; }
  public SegmentsDrawerM SegmentsDrawerM { get; }
  public Selecting<SegmentM> Selected { get; } = new();
  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }

  public bool CanSetAsSamePerson {
    get {
      var p = Selected.Items.Select(x => x.Person).Distinct().ToArray();
      return Selected.Items.Count > 1 && (p.Length > 1 || p[0] == null);
    }
  }

  public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
  public RelayCommand<object> SetSelectedAsUnknownCommand { get; }

  public SegmentsM(SegmentsDA da) {
    DataAdapter = da;
    SegmentsRectsM = new(this);
    SegmentsDrawerM = new(this);

    SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
    SetSelectedAsUnknownCommand = new(
      () => SetAsUnknown(Selected.Items.ToArray()),
      () => Selected.Items.Count > 0);
  }

  public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
    if (!isCtrlOn && !isShiftOn)
      Core.PeopleM.Selected.DeselectAll();

    Selected.Select(segments, segment, isCtrlOn, isShiftOn);
    Core.PeopleM.Selected.Add(Selected.Items.GetPeople());
    OnPropertyChanged(nameof(CanSetAsSamePerson));
  }

  public static void SetSegmentUiSize(double scale) {
    var size = (int)(SegmentSize / scale);
    SegmentUiSize = size;
    SegmentUiFullWidth = size + 6; // + border, margin
  }

  public SegmentM[] GetOneOrSelected(SegmentM one) =>
    one == null
      ? null
      : Selected.Items.Contains(one)
        ? Selected.Items.ToArray()
        : new[] { one };

  /// <summary>
  /// Sets new Person to all Segments that are selected 
  /// or that have the same Unknown Person as some of the selected.
  /// </summary>
  public void SetSelectedAsPerson(SegmentM[] selected, PersonM person) {
    var unknownPeople = selected.GetPeople().Where(x => x.IsUnknown).ToHashSet();
    var segments = selected
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
    var msg = "Do you want to set {0} segment{1} as unknown?".Plural(segments.Length);
    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1) return;
    DataAdapter.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }

  public void ViewMediaItemsWithSegment(object source, SegmentM segment) {
    var items = GetMediaItemsWithSegment(source, segment);
    if (items == null) return;

    Core.MediaViewerM.SetMediaItems(items, segment.MediaItem);
    Core.MainWindowM.IsInViewMode = true;
  }

  private List<MediaItemM> GetMediaItemsWithSegment(object source, SegmentM segment) {
    if (segment == null) return null;

    if (Core.SegmentsView != null && ReferenceEquals(Core.SegmentsView.CvSegments, source))
      return ((CollectionViewGroup<SegmentM>)Core.SegmentsView.CvSegments.LastSelectedRow.Parent).Source
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName)
        .ToList();

    if (segment.Person != null)
      return DataAdapter.All.Where(x => x.Person == segment.Person)
        .GetMediaItems()
        .OrderBy(x => x.FileName)
        .ToList();

    if (ReferenceEquals(SegmentsDrawerM, source))
      return SegmentsDrawerM.Items
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName)
        .ToList();

    return new() { segment.MediaItem };
  }
}