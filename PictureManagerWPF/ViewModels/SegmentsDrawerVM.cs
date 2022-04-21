using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class SegmentsDrawerVM {
    private VirtualizingWrapPanel _panel;

    public SegmentsM SegmentsM { get; }
    public HeaderedListItem<object, string> ToolsTabsItem;
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<RoutedEventArgs> PanelLoadedCommand { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }

    public SegmentsDrawerVM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
      ToolsTabsItem = new(this, "Segments");

      SelectCommand = new(Select);
      PanelLoadedCommand = new(OnPanelLoaded);
      PanelSizeChangedCommand = new(PanelSizeChanged);
    }

    private void OnPanelLoaded(RoutedEventArgs e) {
      _panel = e.Source as VirtualizingWrapPanel;
      DragDropFactory.SetDrag(_panel, CanDrag);
      DragDropFactory.SetDrop(_panel, CanDrop, DoDrop);
    }

    private object CanDrag(MouseEventArgs e) =>
      e.OriginalSource is FrameworkElement { DataContext: SegmentM segmentM }
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (!_panel.Equals(source) && !SegmentsM.SegmentsDrawer.Contains(data))
        return DragDropEffects.Copy;
      if (_panel.Equals(source) && (data as SegmentM[])
          ?.Contains(((e.OriginalSource as FrameworkElement)
            ?.DataContext as SegmentM)) == false)
        return DragDropEffects.Move;
      return DragDropEffects.None;
    }

    private void DoDrop(DragEventArgs e, object source, object data) =>
      SegmentsM.SegmentsDrawerUpdate(data as SegmentM[] ?? new[] { data as SegmentM }, !source.Equals(_panel));

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        SegmentsM.Select(SegmentsM.SegmentsDrawer.Cast<SegmentM>().ToList(), segmentM, e.IsCtrlOn, e.IsShiftOn);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (!e.WidthChanged) return;
      _panel.ReWrap();
    }
  }
}
