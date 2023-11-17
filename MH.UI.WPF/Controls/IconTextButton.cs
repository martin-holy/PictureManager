using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls; 

public class IconTextButton : Button {
  static IconTextButton() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(IconTextButton),
      new FrameworkPropertyMetadata(typeof(IconTextButton)));
  }
}