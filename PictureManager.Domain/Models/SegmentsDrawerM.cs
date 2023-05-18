using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.EventsArgs;
using MH.Utils.HelperClasses;
using PictureManager.Domain.HelperClasses;
using System.Collections.ObjectModel;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsDrawerM : ObservableObject {
    public SegmentsM SegmentsM { get; }
    public ObservableCollection<SegmentM> Items { get; } = new();
    public ObservableCollection<object> GroupedItems { get; } = new();
    public readonly HeaderedListItem<object, string> ToolsTabsItem;
    public CanDragFunc CanDragFunc { get; }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction DoDropAction { get; }

    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }
    public RelayCommand<object> AddSelectedCommand { get; }
    public RelayCommand<object> OpenCommand { get; }

    public SegmentsDrawerM(SegmentsM segmentsM, Core core) {
      SegmentsM = segmentsM;
      ToolsTabsItem = new(this, "Segments");

      CanDragFunc = CanDrag;
      CanDropFunc = CanDrop;
      DoDropAction = DoDrop;

      SelectCommand = new(Select);
      AddSelectedCommand = new(
        () => Update(SegmentsM.Selected.Items.ToArray(), true),
        () => SegmentsM.Selected.Items.Count > 0);
      OpenCommand = new(
        () => {
          Reload();
          core.ToolsTabsM.Activate(ToolsTabsItem, true);
        });
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
      if (!haveSameOrigin && !Items.Contains(data))
        return MH.Utils.DragDropEffects.Copy;
      if (haveSameOrigin && (data as SegmentM[])?.Contains(target as SegmentM) == false)
        return MH.Utils.DragDropEffects.Move;
      return MH.Utils.DragDropEffects.None;
    }

    private void DoDrop(object data, bool haveSameOrigin) =>
      Update(data as SegmentM[] ?? new[] { data as SegmentM }, !haveSameOrigin);

    private void Select(MouseButtonEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        SegmentsM.Select(Items.ToList(), segmentM, e.IsCtrlOn, e.IsShiftOn);
    }

    private void Update(SegmentM[] segments, bool add) {
      if (!add && Core.DialogHostShow(new MessageDialog(
          "Segments Drawer",
          "Do you want to remove segments from drawer?",
          Res.IconQuestion,
          true)) != 1)
        return;

      var count = Items.Count;

      if (add)
        foreach (var segment in segments.Except(Items).ToArray())
          Items.Add(segment);
      else
        foreach (var segment in segments)
          Items.Remove(segment);

      if (count != Items.Count) {
        SegmentsM.DataAdapter.AreTablePropsModified = true;
        Reload();
      }
    }

    public void Remove(SegmentM segment) {
      if (Items.Remove(segment)) {
        SegmentsM.DataAdapter.AreTablePropsModified = true;
        Reload();
      }
    }

    public void Reload() {
      var groups = Items
        .GroupBy(x => x.MediaItem.Folder)
        .OrderBy(x => x.Key.FullPath);
      ItemsGroup group;
      GroupedItems.Clear();

      foreach (var g in groups) {
        group = new();
        group.Info.Add(new ItemsGroupInfoItem(Res.IconFolder, g.Key.Name, g.Key.FullPath));
        GroupedItems.Add(group);

        foreach (var segment in g.OrderBy(x => x.MediaItem.FileName))
          group.Items.Add(segment);
      }
    }
  }
}
