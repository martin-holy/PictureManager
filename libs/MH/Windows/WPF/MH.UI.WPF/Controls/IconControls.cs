using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MH.UI.WPF.Controls;

public enum IconTextBlockShadow { None, Icon, Text, Both }

public class IconTextBlock : Control {
  public static readonly DependencyProperty CompactProperty = DependencyProperty.Register(
    nameof(Compact), typeof(bool), typeof(IconTextBlock));
  public static readonly DependencyProperty TextBorderStyleProperty = DependencyProperty.Register(
    nameof(TextBorderStyle), typeof(Style), typeof(IconTextBlock));
  public static readonly DependencyProperty ShadowProperty = DependencyProperty.Register(
    nameof(Shadow), typeof(IconTextBlockShadow), typeof(IconTextBlock));

  public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }
  public Style TextBorderStyle { get => (Style)GetValue(TextBorderStyleProperty); set => SetValue(TextBorderStyleProperty, value); }
  public IconTextBlockShadow Shadow { get => (IconTextBlockShadow)GetValue(ShadowProperty); set => SetValue(ShadowProperty, value); }
}

public class IconButton : Button;
public class IconTextButton : Button;
public class SlimButton : Button;
public class IconToggleButton : ToggleButton;

public class IconTextBlockItemsControl : ItemsControl;