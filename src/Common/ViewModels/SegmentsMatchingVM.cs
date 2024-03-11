using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.ViewModels;

public sealed class SegmentsMatchingVM : ObservableObject {
  public CanDragFunc CanDragFunc { get; }
  public CollectionViewPeople CvPeople { get; } = new();
  public CollectionViewSegments CvSegments { get; } = new();

  public SegmentsMatchingVM(SegmentS segmentS) {
    CanDragFunc = one => segmentS.GetOneOrSelected(one as SegmentM);
  }

  public static int GetSegmentsToLoadUserInput() {
    var md = new MessageDialog("Segments", "Load segments from ...", Res.IconSegment, true);

    md.Buttons = new DialogButton[] {
      new(md.SetResult(1, Res.IconImage, "Media items"), true),
      new(md.SetResult(2, Res.IconPeople, "People")),
      new(md.SetResult(3, Res.IconSegment, "Segments")) };

    return Dialog.Show(md);
  }

  public static IEnumerable<SegmentM> GetSegments(int mode) {
    switch (mode) {
      case 1:
        var items = Core.VM.MediaViewer.IsVisible
          ? Core.VM.MediaViewer.Current != null
            ? new[] { Core.VM.MediaViewer.Current }
            : Array.Empty<MediaItemM>()
          : Core.VM.MediaItem.Views.Current?.GetSelectedOrAll().ToArray()
            ?? Array.Empty<MediaItemM>();

        return items.Concat(items.GetVideoItems()).GetSegments();
      case 2:
        var people = Core.S.Person.Selected.Items.ToHashSet();

        return Core.R.Segment.All
          .Where(x => x.Person != null && people.Contains(x.Person))
          .OrderBy(x => x.MediaItem.FileName);
      case 3:
        return Core.S.Segment.Selected.Items;
      default:
        return Enumerable.Empty<SegmentM>();
    }
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments) {
    CvSegments.Update(segments, false);
    var p = CvSegments.Root.Source.GetPeople().ToArray();
    var pIn = p.Except(CvPeople.Root.Source).ToArray();
    var pOut = CvPeople.Root.Source.Except(p).ToArray();
    CvPeople.Update(pIn, false);
    CvPeople.Remove(pOut);
  }

  public void Reload(SegmentM[] items) {
    ReloadPeople(items);
    ReloadSegments(items);
  }

  private void ReloadPeople(SegmentM[] items) {
    var source = items.GetPeople().OrderBy(x => x.Name).ToList();
    CvPeople.Reload(source, GroupMode.GroupByRecursive, null, true);
  }

  private void ReloadSegments(SegmentM[] items) {
    var source = items.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = new[] {
      GroupByItems.GetPeopleInGroup(items),
      GroupByItems.GetKeywordsInGroup(items)
    };

    CvSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
  }
}