using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.CustomControls {
  public class ThumbnailsGridControl : Control {
    static ThumbnailsGridControl() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(ThumbnailsGridControl),
        new FrameworkPropertyMetadata(typeof(ThumbnailsGridControl)));
    }

    public override void OnApplyTemplate() {
      if (Template.FindName("PART_Grid", this) is ItemsControl grid)
        grid.PreviewMouseWheel += Grid_OnPreviewMouseWheel;

      base.OnApplyTemplate();
    }

    private static void Grid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && App.Core.ThumbScale < .1) return;
      App.Core.ThumbScale += e.Delta > 0 ? .05 : -.05;
      App.Ui.AppInfo.IsThumbInfoVisible = App.Core.ThumbScale > 0.5;
      App.Core.MediaItems.ThumbsGrid.ResetThumbsSize();
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }
  }
}
