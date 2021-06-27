using PictureManager.Domain;
using System.Collections.ObjectModel;
using System.Windows;

namespace PictureManager.UserControls {
  public partial class InfoPanel {
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(nameof(IconName), typeof(IconName), typeof(InfoPanel));
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(string), typeof(InfoPanel));
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<string>), typeof(InfoPanel));

    public IconName IconName {
      get => (IconName)GetValue(IconNameProperty);
      set => SetValue(IconNameProperty, value);
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
