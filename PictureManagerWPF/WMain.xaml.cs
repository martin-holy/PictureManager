using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Commands;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    public MediaElement VideoThumbnailPreview;

    public CommandsController CommandsController => CommandsController.Instance;

    #region DependencyProperties
    public static readonly DependencyProperty FlyoutMainTreeViewMarginProperty = DependencyProperty.Register(
      nameof(FlyoutMainTreeViewMargin), typeof(Thickness), typeof(WMain));

    public Thickness FlyoutMainTreeViewMargin {
      get => (Thickness) GetValue(FlyoutMainTreeViewMarginProperty);
      set => SetValue(FlyoutMainTreeViewMarginProperty, value);
    }
    #endregion

    public WMain() {
      InitializeComponent();

      // add default ThumbnailsGridControl
      MediaItemsViewModel.AddThumbnailsGridView(TabThumbnailsGrids, App.Core.Model.MediaItems.ThumbsGrid);

      PresentationPanel.Elapsed = delegate {
        Application.Current.Dispatcher?.Invoke(delegate {
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

    private void WMain_OnMouseMove(object sender, MouseEventArgs e) {
      var pos = e.GetPosition(this);
      if (pos.X < 3 && !FlyoutMainTreeView.IsOpen)
        FlyoutMainTreeView.IsOpen = true;
    }

    private void FlyoutMainTreeView_OnMouseLeave(object sender, MouseEventArgs e) {
      if (!FlyoutMainTreeView.IsPinned)
        FlyoutMainTreeView.IsOpen = false;
    }

    public void SetFlyoutMainTreeViewMargin() {
      var top = App.Core.AppInfo.AppMode == AppMode.Browser ? 30 : 0;
      var bottom = FlyoutMainTreeView.IsPinned ? StatusPanel.ActualHeight : 0;
      FlyoutMainTreeViewMargin = new Thickness(0, top, 0, bottom);
    }

    private void MainSplitter_OnDragDelta(object sender, DragDeltaEventArgs e) {
      FlyoutMainTreeView.Width = GridMain.ColumnDefinitions[0].ActualWidth;
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private void MainSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      MainSplitter_OnDragDelta(null, null);
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

    private void TabThumbnailsGrids_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      var grid = (ThumbnailsGrid) ((FrameworkElement) ((TabControl) sender).SelectedItem).DataContext;
      App.Core.Model.MediaItems.ThumbsGrid = grid;
      grid.UpdateSelected();
      App.Core.AppInfo.CurrentMediaItem = grid.Current;
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private void TabThumbnailsGrids_CloseTab(object sender, RoutedEventArgs e) {
      if (!(((FrameworkElement) sender).DataContext is ThumbnailsGrid grid)) return;
      App.Core.MediaItemsViewModel.RemoveThumbnailsGrid(TabThumbnailsGrids, grid);

      // set new SelectedItem and remove TabItem
      var tab = ((FrameworkElement) sender).TryFindParent<TabItem>();
      if (tab == null) return;
      var i = TabThumbnailsGrids.Items.IndexOf(tab);
      TabThumbnailsGrids.SelectedItem = TabThumbnailsGrids.Items[i != 0 ? 0 : 1];
      TabThumbnailsGrids.Items.Remove(tab);
    }

    public void TabThumbnailsGrids_AddTab(object sender, RoutedEventArgs e) {
      MediaItemsViewModel.AddThumbnailsGridView(TabThumbnailsGrids, App.Core.MediaItemsViewModel.AddThumbnailsGridModel());
    }

    private void OnMediaTypesChanged(object sender, RoutedEventArgs e) {
      App.Core.MediaItemsViewModel.ReapplyFilter();
    }
  }
}
