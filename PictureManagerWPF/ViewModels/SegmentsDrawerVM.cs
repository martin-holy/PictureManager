using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class SegmentsDrawerVM {
    public ItemsControl Panel { get; }
    public SegmentsM SegmentsM { get; }
    public HeaderedListItem<object, string> ToolsTabsItem;
    public RelayCommand<ClickEventArgs> SelectCommand { get; }

    public SegmentsDrawerVM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
      Panel = new();
      ToolsTabsItem = new(this, "Segments");

      DragDropFactory.SetDrag(Panel, CanDrag);
      DragDropFactory.SetDrop(Panel, CanDrop, DoDrop);

      SelectCommand = new(Select);
    }

    private object CanDrag(MouseEventArgs e) =>
      e.OriginalSource is FrameworkElement { DataContext: SegmentM segmentM }
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (!Panel.Equals(source) && !SegmentsM.SegmentsDrawer.Contains(data))
        return DragDropEffects.Copy;
      if (Panel.Equals(source) && (data as SegmentM[])
          ?.Contains(((e.OriginalSource as FrameworkElement)
            ?.DataContext as SegmentM)) == false)
        return DragDropEffects.Move;
      return DragDropEffects.None;
    }

    private void DoDrop(DragEventArgs e, object source, object data) {
      foreach (var segment in data as SegmentM[] ?? new[] { data as SegmentM })
        SegmentsM.SegmentsDrawerToggle(segment);
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        SegmentsM.Select(SegmentsM.SegmentsDrawer.ToList(), segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
