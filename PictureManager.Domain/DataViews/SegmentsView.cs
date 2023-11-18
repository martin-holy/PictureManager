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
  private static SegmentsView _inst;
  private static readonly object _lock = new();
  public static SegmentsView Inst { get { lock (_lock) { return GetInst(); } } }
  public static bool IsInst => _inst != null;

  public CollectionViewPeople CvPeople { get; } = new();
  public CollectionViewSegments CvSegments { get; } = new();
  public static RelayCommand<object> OpenCommand { get; } = new(Open);

  private static SegmentsView GetInst() {
    if (_inst != null) return _inst;

    _inst = new();
    Core.PeopleM.AddEvents(_inst.CvPeople);
    Core.SegmentsM.AddEvents(_inst.CvSegments);

    Core.Db.People.ItemDeletedEvent  += (_, e) =>
      _inst.CvPeople.Remove(new[] { e.Data });

    Core.Db.People.PeopleKeywordsChangedEvent += (_, e) =>
      _inst.CvPeople.Update(e.Data);

    Core.Db.Segments.ItemCreatedEvent += (_, e) =>
      _inst.CvSegments.Update(new[] { e.Data });

    Core.Db.Segments.ItemDeletedEvent += (_, e) =>
      _inst.CvSegments.Remove(new[] { e.Data });

    Core.Db.Segments.SegmentsKeywordsChangedEvent += (_, e) =>
      _inst.CvSegments.Update(e.Data);

    Core.Db.Segments.SegmentsPersonChangedEvent += (_, e) => {
      _inst.CvSegments.Update(e.Data.Item2);

      var pIn = e.Data.Item2.GetPeople().ToArray();
      var pOut = e.Data.Item3.Except(pIn).ToArray();
      _inst.CvPeople.Update(pIn);
      _inst.CvPeople.Remove(pOut);
    };

    return _inst;
  }

  private static void Open() {
    var result = GetSegmentsToLoadUserInput();
    if (result < 1) return;
    var segments = GetSegments(result).ToArray();
    Core.MainTabs.Activate(Res.IconSegment, "Segments", Inst);
    if (Core.MediaViewerM.IsVisible)
      Core.MainWindowM.IsFullScreen = false;
    Inst.Reload(segments);
  }

  private static int GetSegmentsToLoadUserInput() {
    var md = new MessageDialog(
      "Segments",
      "Load segments from ...",
      Res.IconSegment,
      true);

    md.Buttons = new DialogButton[] {
      new("Media items", Res.IconImage, md.SetResult(1), true),
      new("People", Res.IconPeople, md.SetResult(2)),
      new("Segments", Res.IconSegment, md.SetResult(3)) };

    return Dialog.Show(md);
  }

  private static IEnumerable<SegmentM> GetSegments(int mode) {
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
    ReloadSegments(items);
    ReloadPeople(items);
  }

  private void ReloadPeople(SegmentM[] items) {
    var source = items.GetPeople().OrderBy(x => x.Name).ToList();
    CvPeople.Reload(source, GroupMode.GroupByRecursive, null, true);
  }

  private void ReloadSegments(SegmentM[] items) {
    var source = items.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = new[] {
      GroupByItems.GetPeopleInGroupFromSegments(items),
      GroupByItems.GetKeywordsInGroupFromSegments(items)
    };

    CvSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
  }
}