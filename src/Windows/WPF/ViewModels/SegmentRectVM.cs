using MH.Utils.BaseClasses;
using PictureManager.Common;
using PictureManager.Common.Models;
using PictureManager.Common.Services;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Windows.WPF.ViewModels;

public sealed class SegmentRectVM : ObservableObject {
  private IInputElement _view;
    
  public SegmentRectS SegmentRectS { get; }
    
  public RelayCommand<RoutedEventArgs> SetViewCommand { get; }
  public static RelayCommand<MouseEventArgs> SetCurrentCommand { get; set; }
  public static RelayCommand<MouseButtonEventArgs> CreateCommand { get; set; }
  public static RelayCommand<MouseEventArgs> EditCommand { get; set; }
  public static RelayCommand EndEditCommand { get; set; }
  public static RelayCommand<SegmentRectM> DeleteCommand { get; set; }

  public SegmentRectVM(SegmentRectS segmentRectS) {
    SegmentRectS = segmentRectS;

    SetViewCommand = new(e => _view = (IInputElement)e.Source);
    SetCurrentCommand = new(SetCurrent);
    CreateCommand = new(Create, () => SegmentRectS.AreVisible);
    EditCommand = new(Edit);
    EndEditCommand = new(SegmentRectS.EndEdit);
    DeleteCommand = new(SegmentRectS.Delete, Res.IconXCross, "Delete");
  }

  private void SetCurrent(MouseEventArgs e) {
    if (e.Source is FrameworkElement fe && (fe.Name.Equals("PART_MovePoint") || fe.Name.Equals("PART_ResizeBorder"))) {
      var pos = e.GetPosition(_view);
      SegmentRectS.SetCurrent((SegmentRectM)fe.DataContext, pos.X, pos.Y);
    }
  }

  private void Create(MouseButtonEventArgs e) {
    if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed) {
      var pos = e.GetPosition(_view);
      SegmentRectS.CreateNew(pos.X, pos.Y);
    }
  }

  private void Edit(MouseEventArgs e) {
    if (SegmentRectS.Current == null) return;

    if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
      SegmentRectS.EndEdit();
      return;
    }

    e.Handled = true;
    var pos = e.GetPosition(_view);
    SegmentRectS.Edit(pos.X, pos.Y);
  }
}