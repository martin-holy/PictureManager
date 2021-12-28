using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace PictureManager {
  public partial class WMain {
    public WMain() {
      InitializeComponent();

      MainSlidePanelsGrid.OnContentLeftWidthChanged += async () => await App.Ui.ThumbnailsGridsVM.ThumbsGridReloadItems();
      RightSlidePanel.CanOpen = () => ToolsTabs.Tabs.Items.Cast<TabItem>().Any(x => x.Visibility == Visibility.Visible);
      ToolsTabs.VideoClips.VideoPlayer = MediaViewer.FullVideo;

      BindingOperations.SetBinding(TreeViewCategories.BtnPinPanel, ToggleButton.IsCheckedProperty,
        new Binding(nameof(SlidePanel.IsPinned)) { Source = SlidePanelMainTreeView });
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      CommandsController.AddCommandBindings(CommandBindings);
      App.Core.WindowsDisplayScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;
      App.Ui.ViewersVM.SetCurrent(App.Core.ViewersM.Current);

      // TODO temporary
      MediaViewer.ApplyTemplate();
      MediaViewer.FullVideo.ApplyTemplate();
      App.Ui.MediaViewer = MediaViewer;

      MediaViewer.FullImage.PropertyChanged += (_, pce) => {
        if (nameof(MediaViewer.FullImage.ActualZoom).Equals(pce.PropertyName))
          App.Ui.StatusPanelVM.OnPropertyChanged(nameof(App.Ui.StatusPanelVM.ActualZoom));
      };
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.MediaItemsM.ModifiedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modified, do you want to save them?", true)) {
        App.Ui.MediaItemsVM.SaveEdit();
      }

      if (App.Db.Changes > 0 &&
          MessageDialog.Show("Database changes", "There are some changes in database, do you want to save them?", true)) {
        App.Db.SaveAllTables();
      }

      App.Db.BackUp();
    }

    private async void WMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      if (App.Ui.AppInfo.AppMode == AppMode.Viewer) return;
      await App.Ui.ThumbnailsGridsVM.ThumbsGridReloadItems();
    }

    private async void OnMediaTypesChanged(object sender, RoutedEventArgs e) => await App.Ui.ThumbnailsGridsVM.ReapplyFilter();
  }
}
