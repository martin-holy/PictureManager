using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataViews;

public sealed class SegmentsView {
  public CollectionViewPeople CvPeople { get; } = new();
  public CollectionViewSegments CvSegments { get; } = new();

  public static int GetSegmentsToLoadUserInput() {
    var md = new MessageDialog("Segments", "Load segments from ...", Res.IconSegment, true);

    md.Buttons = new DialogButton[] {
      new("Media items", Res.IconImage, md.SetResult(1), true),
      new("People", Res.IconPeople, md.SetResult(2)),
      new("Segments", Res.IconSegment, md.SetResult(3)) };

    return Dialog.Show(md);
  }

  public static IEnumerable<SegmentM> GetSegments(int mode) {
    switch (mode) {
      case 1:
        return (Core.MediaViewerM.IsVisible
                 ? Core.MediaViewerM.Current?.GetSegments()
                 : Core.MediaItemsViews.Current?.GetSelectedOrAll().GetSegments())
               ?? Enumerable.Empty<SegmentM>();
      case 2:
        var people = Core.PeopleM.Selected.Items.ToHashSet();

        return Core.Db.Segments.All
          .Where(x => x.Person != null && people.Contains(x.Person))
          .OrderBy(x => x.MediaItem.FileName);
      case 3:
        return Core.SegmentsM.Selected.Items;
      default:
        return Enumerable.Empty<SegmentM>();
    }
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