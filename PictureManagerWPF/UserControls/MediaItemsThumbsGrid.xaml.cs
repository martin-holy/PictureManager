using PictureManager.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.ViewModels;

namespace PictureManager.UserControls {
  public partial class MediaItemsThumbsGrid {
    public MediaItemsThumbsGrid() {
      InitializeComponent();
      DragDropFactory.SetDrag(this, CanDrag, DataFormats.FileDrop);
    }

    private object CanDrag(MouseEventArgs e) {
      var data = App.Core.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToArray();
      return data.Length == 0 ? null : data;
    }

    private async void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && App.Core.ThumbScale < .1) return;
      App.Core.ThumbScale += e.Delta > 0 ? .05 : -.05;
      App.Ui.AppInfo.IsThumbInfoVisible = App.Core.ThumbScale > 0.5;
      App.Core.MediaItems.ThumbsGrid.ResetThumbsSize();
      await App .Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private async void Refresh(object sender, RoutedEventArgs e) {
      if (App.Core.MediaItems.ThumbsGrid != null)
        await App.Core.MediaItems.ThumbsGrid.ReloadFilteredItems(
          MediaItemsViewModel.Filter(App.Core.MediaItems.ThumbsGrid.LoadedItems));
      await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }
  }
}
