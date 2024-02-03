using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls;

public class IconButton : Button {
  static IconButton() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(IconButton),
      new FrameworkPropertyMetadata(typeof(IconButton)));
  }
}