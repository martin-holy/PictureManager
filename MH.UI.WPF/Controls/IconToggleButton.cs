using System.Windows;
using System.Windows.Controls.Primitives;

namespace MH.UI.WPF.Controls;

public class IconToggleButton : ToggleButton {
  static IconToggleButton() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(IconToggleButton),
      new FrameworkPropertyMetadata(typeof(IconToggleButton)));
  }
}