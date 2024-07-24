using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Segment;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Windows.WPF.ViewModels;

public sealed class SegmentRectVM : ObservableObject {
  private IInputElement _view = null!;
    
  public SegmentRectS SegmentRectS { get; }
    
  public RelayCommand<RoutedEventArgs> SetViewCommand { get; }
  public static RelayCommand<MouseEventArgs> SetCurrentCommand { get; set; } = null!;
  public static RelayCommand<MouseButtonEventArgs> CreateCommand { get; set; } = null!;
  public static RelayCommand<MouseEventArgs> EditCommand { get; set; } = null!;
  public static RelayCommand EndEditCommand { get; set; } = null!;
  public static RelayCommand<SegmentRectM> DeleteCommand { get; set; } = null!;

  public SegmentRectVM(SegmentRectS segmentRectS) {
    SegmentRectS = segmentRectS;

    SetViewCommand = new(x => _view = (IInputElement)x!.Source, x => x != null);
    SetCurrentCommand = new(SetCurrent);
    CreateCommand = new(Create, () => SegmentRectS.AreVisible);
    EditCommand = new(Edit);
    EndEditCommand = new(SegmentRectS.EndEdit);
    DeleteCommand = new(x => SegmentRectS.Delete(x!), x => x != null, MH.UI.Res.IconXCross, "Delete");
  }

  private void SetCurrent(MouseEventArgs? e) {
    if (e?.Source is FrameworkElement fe && (fe.Name.Equals("PART_MovePoint") || fe.Name.Equals("PART_ResizeBorder"))) {
      var pos = e.GetPosition(_view);
      SegmentRectS.SetCurrent((SegmentRectM)fe.DataContext, pos.X, pos.Y);
    }
  }

  private void Create(MouseButtonEventArgs? e) {
    if (e != null && ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed)) {
      var pos = e.GetPosition(_view);
      SegmentRectS.CreateNew(pos.X, pos.Y);
    }
  }

  private void Edit(MouseEventArgs? e) {
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