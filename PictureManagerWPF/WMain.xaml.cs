using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace PictureManager {
  public partial class WMain {
    public MediaElement VideoThumbnailPreview { get; }

    public WMain() {
      InitializeComponent();

      VideoThumbnailPreview = new() {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      VideoThumbnailPreview.MediaEnded += (o, args) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement)o).Position = TimeSpan.FromMilliseconds(1);
      };

      MainSlidePanelsGrid.OnContentLeftWidthChanged += async () => await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
      RightSlidePanel.CanOpen = () => ToolsTabs.Tabs.Items.Cast<TabItem>().Any(x => x.Visibility == Visibility.Visible);
      ToolsTabs.VideoClips.VideoPlayer = MediaViewer.FullVideo;
      MainTabs.OnAddTab += () => App.Ui.MediaItemsViewModel.AddThumbsTab();

      BindingOperations.SetBinding(TreeViewCategories.BtnPinPanel, ToggleButton.IsCheckedProperty,
        new Binding(nameof(SlidePanel.IsPinned)) { Source = SlidePanelMainTreeView });

      StatusPanel.SizeChanged += delegate {
        SlidePanelMainTreeView.BorderMargin = new(0, 0, 0, StatusPanel.ActualHeight);
      };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      CommandsController.AddCommandBindings(CommandBindings);
      CommandsController.AddInputBindings();
      App.Core.WindowsDisplayScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;
      MenuViewers.Header = App.Core.CurrentViewer?.Name ?? "Viewer";
      App.Ui.MediaItemsViewModel.RegisterEvents();
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.MediaItems.ModifiedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modified, do you want to save them?", true)) {
        MetadataCommands.Save();
      }

      if (App.Db.Changes > 0 &&
          MessageDialog.Show("Database changes", "There are some changes in database, do you want to save them?", true)) {
        App.Db.SaveAllTables();
      }

      App.Db.BackUp();
    }

    private async void WMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      if (App.Ui.AppInfo.AppMode == AppMode.Viewer) return;
      await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private async void FiltersPanel_ClearFilters(object sender, MouseButtonEventArgs e) => await App.Ui.ClearFilters();

    private async void OnMediaTypesChanged(object sender, RoutedEventArgs e) => await App.Ui.MediaItemsViewModel.ReapplyFilter();
  }
}
