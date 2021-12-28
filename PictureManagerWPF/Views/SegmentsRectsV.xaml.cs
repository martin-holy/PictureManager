using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.Views {
  public partial class SegmentsRectsV {

    public static readonly DependencyProperty SegmentsRectsMProperty = 
      DependencyProperty.Register(nameof(SegmentsRectsM), typeof(SegmentsRectsM), typeof(SegmentsRectsV));

    public SegmentsRectsM SegmentsRectsM {
      get => (SegmentsRectsM)GetValue(SegmentsRectsMProperty);
      set => SetValue(SegmentsRectsMProperty, value);
    }

    public RelayCommand<SegmentRectM> DeleteCommand { get; }

    public SegmentsRectsV() {
      InitializeComponent();

      DeleteCommand = new(Delete, segmentRect => segmentRect != null);
    }

    private void SegmentRect_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if (e.Source is FrameworkElement fe && (fe.Name.Equals("MovePoint") || fe.Name.Equals("ResizeBorder")))
        SegmentsRectsM.SetCurrent((SegmentRectM)fe.DataContext, fe.Name.Equals("MovePoint"));
    }

    public void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed) {
        var pos = e.GetPosition(this);
        SegmentsRectsM.CreateNew(pos.X, pos.Y);
      }
    }

    public void OnPreviewMouseMove(object sender, MouseEventArgs e) {
      if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
        if (SegmentsRectsM.Current != null)
          SegmentsRectsM.EndEdit();
        return;
      }

      if (SegmentsRectsM.Current != null) {
        e.Handled = true;
        var pos = e.GetPosition(this);
        SegmentsRectsM.StartEdit((int)pos.X, (int)pos.Y);
      }
    }

    public void OnPreviewMouseUp(object sender, MouseButtonEventArgs e) =>
      SegmentsRectsM.EndEdit();

    private void Delete(SegmentRectM segmentRect) {
      if (!MessageDialog.Show("Delete Segment", "Do you really want to delete this segment?", true)) return;
      SegmentsRectsM.Delete(segmentRect);
    }
  }
}
