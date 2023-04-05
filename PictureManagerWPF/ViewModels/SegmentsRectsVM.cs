using System.Windows;
using System.Windows.Input;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class SegmentsRectsVM : ObservableObject {
    private IInputElement _view;
    private bool _areVisible;

    public SegmentsRectsM SegmentsRectsM { get; }
    public bool AreVisible { get => _areVisible; set { _areVisible = value; OnPropertyChanged(); } }

    public RelayCommand<RoutedEventArgs> SetViewCommand { get; }
    public RelayCommand<MouseEventArgs> SetCurrentCommand { get; }
    public RelayCommand<MouseButtonEventArgs> CreateCommand { get; }
    public RelayCommand<MouseEventArgs> EditCommand { get; }
    public RelayCommand<object> EndEditCommand { get; }
    public RelayCommand<SegmentRectM> DeleteCommand { get; }

    public SegmentsRectsVM(SegmentsRectsM segmentsRectsM) {
      SegmentsRectsM = segmentsRectsM;

      SetViewCommand = new(e => _view = (IInputElement)e.Source);
      SetCurrentCommand = new(SetCurrent);
      CreateCommand = new(Create, () => AreVisible);
      EditCommand = new(Edit);
      EndEditCommand = new(() => SegmentsRectsM.EndEdit());
      DeleteCommand = new(SegmentsRectsM.Delete);
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
      SegmentsRectsM.StartEdit((int)pos.X, (int)pos.Y);
    }
  }
}
