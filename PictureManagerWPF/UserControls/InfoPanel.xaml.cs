using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using MH.UI.WPF.Controls;

namespace PictureManager.UserControls {
  public partial class InfoPanel {
    public static readonly DependencyProperty IconSourceProperty = Icon.SourceProperty.AddOwner(typeof(InfoPanel));
    public static readonly DependencyProperty IconFillProperty = Icon.FillProperty.AddOwner(typeof(InfoPanel));
    public static readonly DependencyProperty IconSizeProperty = Icon.SizeProperty.AddOwner(typeof(InfoPanel));
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(string), typeof(InfoPanel));
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<string>), typeof(InfoPanel));

    public PathGeometry IconSource {
      get => (PathGeometry)GetValue(IconSourceProperty);
      set => SetValue(IconSourceProperty, value);
    }

    public Brush IconFill {
      get => (Brush)GetValue(IconFillProperty);
      set => SetValue(IconFillProperty, value);
    }

    public double IconSize {
      get => (double)GetValue(IconSizeProperty);
      set => SetValue(IconSizeProperty, value);
    }

    public string Item {
      get => (string)GetValue(ItemProperty);
      set => SetValue(ItemProperty, value);
    }

    public ObservableCollection<string> Items {
      get => (ObservableCollection<string>)GetValue(ItemsProperty);
      set => SetValue(ItemsProperty, value);
    }

    public InfoPanel() {
      InitializeComponent();
    }
  }
}
