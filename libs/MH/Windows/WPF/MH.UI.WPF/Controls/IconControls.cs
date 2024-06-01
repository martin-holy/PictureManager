using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MH.UI.WPF.Controls;

public class IconTextBlock : Control {
  public static readonly DependencyProperty CompactProperty = DependencyProperty.Register(
    nameof(Compact), typeof(bool), typeof(IconTextBlock));

  public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }
}

public class IconButton : Button;
public class IconTextButton : Button;
public class SlimButton : Button;
public class IconToggleButton : ToggleButton;

public class IconTextBlockItemsControl : ItemsControl;