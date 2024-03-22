using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MH.UI.WPF.Controls;

public class PopupSlider : Slider {
  public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
    nameof(Content), typeof(Button), typeof(PopupSlider));

  public Button Content {
    get => (Button)GetValue(ContentProperty);
    set => SetValue(ContentProperty, value);
  }

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();

    if (Content == null || GetTemplateChild("PART_Popup") is not Popup popup) return;

    Content.Click += delegate { popup.IsOpen = true; };
    popup.PreviewMouseUp += delegate { popup.IsOpen = false; };
    popup.CustomPopupPlacementCallback += (size, targetSize, _) => {
      var x = targetSize.Width / 2 - size.Width / 2;
      return new[] { new CustomPopupPlacement(new(x, targetSize.Height), PopupPrimaryAxis.Vertical) };
    };
  }
}