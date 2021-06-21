using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class MediaItemsThumbsGrid {
    public MediaItemsThumbsGrid() {
      InitializeComponent();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && App.Core.ThumbScale < .1) return;
      App.Core.ThumbScale += e.Delta > 0 ? .05 : -.05;
      App.Ui.AppInfo.IsThumbInfoVisible = App.Core.ThumbScale > 0.5;
      App.Core.MediaItems.ThumbsGrid.ResetThumbsSize();
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private void Refresh(object sender, RoutedEventArgs e) {
      App.Core.MediaItems.ThumbsGrid?.ReloadFilteredItems();
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }
  }
}
