using PictureManager.Commands;
using PictureManager.Domain.Models;
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

    public override void OnApplyTemplate() {
      PreviewMouseDoubleClick += (o, e) => MediaItemsCommands.ViewMediaItemsWithFaceCommand.Execute((Face)DataContext, this);

      base.OnApplyTemplate();
    }
  }
}
