using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.ViewModels {
  public sealed class SegmentsRectsVM : ObservableObject {
    private IInputElement _view;
    
    public SegmentsRectsM SegmentsRectsM { get; }
    
    public RelayCommand<RoutedEventArgs> SetViewCommand { get; }
    public static RelayCommand<MouseEventArgs> SetCurrentCommand { get; set; }
    public static RelayCommand<MouseButtonEventArgs> CreateCommand { get; set; }
    public static RelayCommand<MouseEventArgs> EditCommand { get; set; }
    public static RelayCommand EndEditCommand { get; set; }
    public static RelayCommand<SegmentRectM> DeleteCommand { get; set; }

    public SegmentsRectsVM(SegmentsRectsM segmentsRectsM) {
      SegmentsRectsM = segmentsRectsM;

      SetViewCommand = new(e => _view = (IInputElement)e.Source);
      SetCurrentCommand = new(SetCurrent);
      CreateCommand = new(Create, () => SegmentsRectsM.AreVisible);
      EditCommand = new(Edit);
      EndEditCommand = new(SegmentsRectsM.EndEdit);
      DeleteCommand = new(SegmentsRectsM.Delete, Res.IconXCross, "Delete");
    }

    private void SetCurrent(MouseEventArgs e) {
      if (e.Source is FrameworkElement fe && (fe.Name.Equals("PART_MovePoint") || fe.Name.Equals("PART_ResizeBorder"))) {
        var pos = e.GetPosition(_view);
        SegmentsRectsM.SetCurrent((SegmentRectM)fe.DataContext, pos.X, pos.Y);
      }
    }

    private void Create(MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed) {
        var pos = e.GetPosition(_view);
        SegmentsRectsM.CreateNew(pos.X, pos.Y);
      }
    }

    private void Edit(MouseEventArgs e) {
      if (SegmentsRectsM.Current == null) return;

      if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
        SegmentsRectsM.EndEdit();
        return;
      }

      e.Handled = true;
      var pos = e.GetPosition(_view);
      SegmentsRectsM.Edit(pos.X, pos.Y);
    }
  }
}
