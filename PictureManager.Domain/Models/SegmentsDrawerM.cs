using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsDrawerM : CollectionView<SegmentM> {
    private readonly SegmentsM _segmentsM;

    public ObservableCollection<SegmentM> Items { get; } = new();
    public readonly HeaderedListItem<object, string> ToolsTabsItem;
    public CanDragFunc CanDragFunc { get; }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction DoDropAction { get; }

    public RelayCommand<object> AddSelectedCommand { get; }
    public RelayCommand<object> OpenCommand { get; }

    public SegmentsDrawerM(SegmentsM segmentsM, Core core) {
      _segmentsM = segmentsM;
      ToolsTabsItem = new(this, "Segments");

      CanDragFunc = CanDrag;
      CanDropFunc = CanDrop;
      DoDropAction = DoDrop;

      AddSelectedCommand = new(
        () => Update(_segmentsM.Selected.Items.ToArray(), true),
        () => _segmentsM.Selected.Items.Count > 0);
      OpenCommand = new(() => Open(core.ToolsTabsM));
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
      if (!add && Core.DialogHostShow(new MessageDialog(
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
      ReGroupItems(new[] { segment }, true);
    }

    private void Open(ToolsTabsM tt) {
      var gbi = GetGroupByItems(Items).ToArray();
      var source = Items
        .OrderBy(x => x.MediaItem.Folder.FullPath)
        .ThenBy(x => x.MediaItem.FileName)
        .ToList();

      SetRoot(Res.IconSegment, "Segments", source);
      Root.GroupMode = GroupMode.GroupByRecursive;
      Root.GroupByItems = gbi.Length == 0 ? null : gbi;
      Root.GroupIt();
      Root.ExpandAll();

      tt.Activate(ToolsTabsItem, true);
    }

    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<SegmentM> source, SegmentM item, bool isCtrlOn, bool isShiftOn) =>
      _segmentsM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    public override IEnumerable<CollectionViewGroupByItem<SegmentM>> GetGroupByItems(IEnumerable<SegmentM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<SegmentM>>();
      top.AddRange(GroupByItems.GetFoldersFromSegments(src));
      top.AddRange(GroupByItems.GetKeywordsFromSegments(src));

      return top;
    }

    public override string ItemOrderBy(SegmentM item) =>
      item.MediaItem.FileName;
  }
}
