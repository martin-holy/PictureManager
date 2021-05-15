using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class StatusPanel {

    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
      nameof(IsPinned), typeof(bool), typeof(StatusPanel), new UIPropertyMetadata(true));

    public bool IsPinned {
      get => (bool)GetValue(IsPinnedProperty);
      set => SetValue(IsPinnedProperty, value);
    }

    public StatusPanel() {
      InitializeComponent();
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      IsPinned = !IsPinned;
    }
  }
}
