using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class FaceControl : Control {
    public static readonly DependencyProperty IsCheckmarkVisibleProperty = DependencyProperty.Register(nameof(IsCheckmarkVisible), typeof(bool), typeof(FaceControl), new PropertyMetadata(false));

    public bool IsCheckmarkVisible {
      get => (bool)GetValue(IsCheckmarkVisibleProperty);
      set => SetValue(IsCheckmarkVisibleProperty, value);
    }

    static FaceControl() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(FaceControl), new FrameworkPropertyMetadata(typeof(FaceControl)));
    }
  }
}
