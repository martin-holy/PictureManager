using System.Windows;
using PictureManager.Domain;

namespace PictureManager.UserControls {
  public partial class IconRect {
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(
      nameof(IconName),typeof(IconName), typeof(IconRect));

    public IconName IconName {
      get => (IconName)GetValue(IconNameProperty);
      set => SetValue(IconNameProperty, value);
    }
    public IconRect() {
      InitializeComponent();
    }
  }
}
