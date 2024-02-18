using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.Services;

public sealed class SegmentS : ObservableObject {
  public SegmentR DataAdapter { get; set; }
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

  public SegmentS(SegmentR r) {
    DataAdapter = r;
    SegmentsRectsM = new(this);
    SegmentsDrawerM = new(this);
  }

  public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
    if (!isCtrlOn && !isShiftOn)
      Core.S.Person.Selected.DeselectAll();

    Selected.Select(segments, segment, isCtrlOn, isShiftOn);
    Core.S.Person.Selected.Add(Selected.Items.GetPeople());
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

    Core.S.Person.MergePeople(person, unknownPeople.ToArray());
    DataAdapter.ChangePerson(person, segments, people);
  }

  public void SetSelectedAsSamePerson(SegmentM[] items) {
    if (!CanSetAsSamePerson) return;

    PersonM newPerson;
    SegmentM[] toUpdate;
    var people = items.GetPeople().OrderBy(x => x.Name).ToArray();

    if (people.Length == 0) {
      newPerson = Core.R.Person.ItemCreateUnknown();
      toUpdate = items;
    }
    else if (people.Length == 1) {
      newPerson = people[0];
      toUpdate = items.Where(x => x.Person == null).ToArray();
    }
    else {
      if (!MergePeopleDialogM.Open(Core.S.Person, this, people)) return;
      newPerson = MergePeopleDialogM.Person;
      toUpdate = MergePeopleDialogM.SegmentsToUpdate;
    }

    Core.S.Person.Selected.DeselectAll();
    var affectedPeople = people.Concat(new[] { newPerson }).Distinct().ToArray();
    DataAdapter.ChangePerson(newPerson, toUpdate, affectedPeople);
  }

  public void ViewMediaItemsWithSegment(object source, SegmentM segment) {
    var items = GetMediaItemsWithSegment(source, segment).ToArray();
    var rmis = items.OfType<RealMediaItemM>()
      .Concat(items.OfType<VideoItemM>().Select(x => x.Video))
      .Distinct().Cast<MediaItemM>().ToList();

    if (rmis.Count == 0) return;
    var current = segment.MediaItem is VideoItemM vi ? vi.Video : segment.MediaItem;
    Core.MediaViewerM.SetMediaItems(rmis, current);
    Core.VM.MainWindow.IsInViewMode = true;
  }

  private IEnumerable<MediaItemM> GetMediaItemsWithSegment(object source, SegmentM segment) {
    if (segment == null) return Enumerable.Empty<MediaItemM>();

    if (Core.SegmentsView != null && ReferenceEquals(Core.SegmentsView.CvSegments, source))
      return ((CollectionViewGroup<SegmentM>)Core.SegmentsView.CvSegments.LastSelectedRow.Parent).Source
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName);

    if (segment.Person != null)
      return DataAdapter.All.Where(x => x.Person == segment.Person)
        .GetMediaItems()
        .OrderBy(x => x.FileName);

    if (ReferenceEquals(SegmentsDrawerM, source))
      return SegmentsDrawerM.Items
        .GetMediaItems()
        .OrderBy(x => x.Folder.FullPath)
        .ThenBy(x => x.FileName);

    return new[] { segment.MediaItem };
  }
}