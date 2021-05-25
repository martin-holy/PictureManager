using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    public MediaElement VideoThumbnailPreview;
    public CommandsController CommandsController => CommandsController.Instance;
    
    public WMain() {
      InitializeComponent();

      // add default ThumbnailsGridControl
      ThumbnailsTabs.AddTab(App.Core.Model.MediaItems.ThumbsGrid);

      PresentationPanel.Elapsed = delegate {
        Application.Current.Dispatcher?.Invoke(delegate {
          if (PresentationPanel.IsPaused) return;
          if (MediaItemsCommands.CanNext())
            CommandsController.MediaItemsCommands.Next();
          else
            PresentationPanel.Stop();
        });
      };

      FullMedia.ApplyTemplate();
      FullMedia.MediaItemClips.Add(App.Core.MediaItemClipsCategory);
      FullMedia.RepeatEnded += delegate {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      VideoThumbnailPreview = new MediaElement {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      VideoThumbnailPreview.MediaEnded += (o, args) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement) o).Position = TimeSpan.FromMilliseconds(1);
      };

      MainSlidePanelsGrid.OnContentLeftWidthChanged += delegate { App.Core.MediaItemsViewModel.ThumbsGridReloadItems(); };

      BindingOperations.SetBinding(TreeViewCategories.BtnPinPanel, ToggleButton.IsCheckedProperty,
        new Binding(nameof(SlidePanel.IsPinned)) { Source = SlidePanelMainTreeView });

      StatusPanel.SizeChanged += delegate {
        SlidePanelMainTreeView.BorderMargin = new Thickness(0,0,0, StatusPanel.ActualHeight);
        FullMedia.ClipsPanel.BorderMargin = new Thickness(0, 0, 0, StatusPanel.ActualHeight);
      };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      CommandsController.AddCommandBindings(CommandBindings);
      CommandsController.AddInputBindings();
      App.Core.Model.WindowsDisplayScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;
      MenuViewers.Header = App.Core.Model.CurrentViewer?.Title ?? "Viewer";
    }

    public void SetMediaItemSource(bool decoded = false) {
      var current = App.Core.Model.MediaItems.ThumbsGrid.Current;
      switch (current.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(current, decoded);
          App.Core.MediaItemClipsCategory.SetMediaItem(null);
          FullMedia.SetSource(null);
          break;
        }
        case MediaType.Video: {
          App.Core.MediaItemClipsCategory.SetMediaItem(current);
          FullMedia.SetSource(current);
          break;
        }
      }
    }

    private void PanelFullScreen_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2) {
        CommandsController.WindowCommands.SwitchToBrowser();
      }
    }

    private void PanelFullScreen_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (MediaItemsCommands.CanNext())
          CommandsController.MediaItemsCommands.Next();
      }
      else {
        if (MediaItemsCommands.CanPrevious())
          CommandsController.MediaItemsCommands.Previous();
      }
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.Model.MediaItems.ModifiedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modified, do you want to save them?", true)) {
        CommandsController.MetadataCommands.Save();
      }
      App.Core.Model.Sdb.SaveAllTables();
    }

    private void WMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      if (App.Core.AppInfo.AppMode == AppMode.Viewer) return;
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private void FiltersPanel_ClearFilters(object sender, MouseButtonEventArgs e) {
      App.Core.ClearFilters();
    }

    private void OnMediaTypesChanged(object sender, RoutedEventArgs e) {
      App.Core.MediaItemsViewModel.ReapplyFilter();
    }
  }
}
