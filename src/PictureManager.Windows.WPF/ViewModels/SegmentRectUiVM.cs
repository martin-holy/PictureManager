using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Segment;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Windows.WPF.ViewModels;

public sealed class SegmentRectUiVM : ObservableObject {
  private IInputElement _view = null!;
    
  public SegmentRectS SegmentRectS { get; }
  public SegmentRectVM SegmentRectVM { get; }

  public RelayCommand<RoutedEventArgs> SetViewCommand { get; }
  public static RelayCommand<MouseEventArgs> SetCurrentCommand { get; set; } = null!;
  public static RelayCommand<MouseButtonEventArgs> CreateCommand { get; set; } = null!;
  public static RelayCommand<MouseEventArgs> EditCommand { get; set; } = null!;
  public static RelayCommand EndEditCommand { get; set; } = null!;
  public static AsyncRelayCommand<SegmentRectM> DeleteCommand { get; set; } = null!;

  public SegmentRectUiVM(SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) {
    SegmentRectVM = segmentRectVM;
    SegmentRectS = segmentRectS;

    SetViewCommand = new(x => _view = (IInputElement)x!.Source, x => x != null);
    SetCurrentCommand = new(_setCurrent);
    CreateCommand = new(_create, () => SegmentRectVM.ShowOverMediaItem);
    EditCommand = new(_edit);
    EndEditCommand = new(SegmentRectS.EndEdit);
    DeleteCommand = new((x, _) => SegmentRectS.Delete(x!), x => x != null, MH.UI.Res.IconXCross, "Delete");
  }

  private void _setCurrent(MouseEventArgs? e) {
    if (e?.Source is FrameworkElement fe && (fe.Name.Equals("PART_MovePoint") || fe.Name.Equals("PART_ResizeBorder"))) {
      var pos = e.GetPosition(_view);
      SegmentRectS.SetCurrent((SegmentRectM)fe.DataContext, pos.X, pos.Y);
    }
  }

  private void _create(MouseButtonEventArgs? e) {
    if (e != null && ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed)) {
      var pos = e.GetPosition(_view);
      SegmentRectS.CreateNew(pos.X, pos.Y);
    }
  }

  private void _edit(MouseEventArgs? e) {
    if (e == null || SegmentRectS.Current == null) return;

    if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
      SegmentRectS.EndEdit();
      return;
    }

    e.Handled = true;
    var pos = e.GetPosition(_view);
    SegmentRectS.Edit(pos.X, pos.Y);
  }
}