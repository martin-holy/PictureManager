using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using System.Collections.ObjectModel;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models; 

public sealed class SegmentsDrawerM : CollectionViewSegments {
  private readonly SegmentsM _segmentsM;

  public ObservableCollection<SegmentM> Items { get; } = new();
  public CanDragFunc CanDragFunc { get; }
  public CanDropFunc CanDropFunc { get; }
  public DoDropAction DoDropAction { get; }

  public RelayCommand<object> AddSelectedCommand { get; }
  public RelayCommand<object> OpenCommand { get; }

  public SegmentsDrawerM(SegmentsM segmentsM) {
    _segmentsM = segmentsM;

    CanDragFunc = CanDrag;
    CanDropFunc = CanDrop;
    DoDropAction = DoDrop;

    AddSelectedCommand = new(
      () => Update(_segmentsM.Selected.Items.ToArray(), true),
      () => _segmentsM.Selected.Items.Count > 0);
    OpenCommand = new(() => Open(Core.ToolsTabsM));
  }

  private object CanDrag(object source) =>
    source is SegmentM segmentM
      ? _segmentsM.GetOneOrSelected(segmentM)
      : null;

  private DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
    if (!haveSameOrigin && !Items.Contains(data))
      return DragDropEffects.Copy;
    if (haveSameOrigin && (data as SegmentM[])?.Contains(target as SegmentM) == false)
      return DragDropEffects.Move;
    return DragDropEffects.None;
  }

  private void DoDrop(object data, bool haveSameOrigin) =>
    Update(data as SegmentM[] ?? new[] { data as SegmentM }, !haveSameOrigin);

  private void Update(SegmentM[] segments, bool add) {
    if (!add && Dialog.Show(new MessageDialog(
          "Segments Drawer",
          "Do you want to remove segments from drawer?",
          Res.IconQuestion,
          true)) != 1)
      return;

    var count = Items.Count;

    if (add) {
      var toAdd = Items.Concat(segments.Except(Items)).ToArray();
      Items.Clear();

      foreach (var segment in toAdd)
        Items.Add(segment);
    }
    else
      foreach (var segment in segments)
        Items.Remove(segment);

    if (count != Items.Count) {
      _segmentsM.DataAdapter.AreTablePropsModified = true;
      ReGroupItems(segments, !add);
    }
  }

  public void Remove(SegmentM segment) {
    if (!Items.Remove(segment)) return;
    _segmentsM.DataAdapter.AreTablePropsModified = true;
    Remove(new[] { segment });
  }

  private void Open(ToolsTabsM tt) {
    var source = Items
      .OrderBy(x => x.MediaItem.Folder.FullPath)
      .ThenBy(x => x.MediaItem.FileName)
      .ToList();
    var groupByItems = GroupByItems.GetFolders(source).ToArray();

    Reload(source, GroupMode.GroupByRecursive, groupByItems, true);
    tt.Activate(Res.IconSegment, "Segments", this);
    tt.Open();
  }
}