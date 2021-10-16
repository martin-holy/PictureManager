using System.Windows;
using PictureManager.Domain;

namespace PictureManager.UserControls {
  public partial class CategoryGroupIconControl {

    public static readonly DependencyProperty CategoryProperty =
      DependencyProperty.Register(nameof(Category), typeof(Category), typeof(CategoryGroupIconControl));

    public int Category {
      get => (int)GetValue(CategoryProperty);
      set => SetValue(CategoryProperty, value);
    }

    public CategoryGroupIconControl() {
      InitializeComponent();
    }
  }
}
