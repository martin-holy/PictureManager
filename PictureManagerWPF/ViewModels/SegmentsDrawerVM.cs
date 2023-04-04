using System.Linq;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Domain.Models;
using static MH.Utils.DragDropHelper;

namespace PictureManager.ViewModels {
  public sealed class SegmentsDrawerVM : ObservableObject {
    private VirtualizingWrapPanel _panel;
    private bool _reWrapItems;

    public SegmentsM SegmentsM { get; }
    public readonly HeaderedListItem<object, string> ToolsTabsItem;
    public CanDragFunc CanDragFunc { get; }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction DoDropAction { get; }
    public bool ReWrapItems { get => _reWrapItems; set { _reWrapItems = value; OnPropertyChanged(); } }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<object> PanelWidthChangedCommand { get; }

    public SegmentsDrawerVM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
      ToolsTabsItem = new(this, "Segments");

      CanDragFunc = CanDrag;
      CanDropFunc = CanDrop;
      DoDropAction = DoDrop;

      SelectCommand = new(Select);
      PanelWidthChangedCommand = new(() => ReWrapItems = true);
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
      if (!haveSameOrigin && !SegmentsM.SegmentsDrawer.Contains(data))
        return MH.Utils.DragDropEffects.Copy;
      if (haveSameOrigin && (data as SegmentM[])?.Contains(target as SegmentM) == false)
        return MH.Utils.DragDropEffects.Move;
      return MH.Utils.DragDropEffects.None;
    }

    private void DoDrop(object data, bool haveSameOrigin) =>
      SegmentsM.SegmentsDrawerUpdate(data as SegmentM[] ?? new[] { data as SegmentM }, !haveSameOrigin);

    private void Select(ClickEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        SegmentsM.Select(SegmentsM.SegmentsDrawer.Cast<SegmentM>().ToList(), segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
