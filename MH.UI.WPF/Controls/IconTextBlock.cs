using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls; 

public class IconTextBlock : Control {
  static IconTextBlock() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(IconTextBlock),
      new FrameworkPropertyMetadata(typeof(IconTextBlock)));
  }
}