﻿using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace PictureManager {
  public partial class WMain {
    public MediaElement VideoThumbnailPreview;

    public WMain() {
      InitializeComponent();

      // Add Default MainTabs Tab
      MainTabs.AddTab();

      PresentationPanel.Elapsed = delegate {
        App.Core.RunOnUiThread(() => {
          if (PresentationPanel.IsPaused) return;
          if (MediaItemsCommands.CanNext())
            MediaItemsCommands.Next();
          else
            PresentationPanel.Stop();
        });
      };

      FullMedia.ApplyTemplate();
      FullMedia.MediaItemClips.Add(App.Ui.MediaItemClipsCategory);
      FullMedia.RepeatEnded += delegate {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      VideoThumbnailPreview = new() {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      VideoThumbnailPreview.MediaEnded += (o, args) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement)o).Position = TimeSpan.FromMilliseconds(1);
      };

      MainSlidePanelsGrid.OnContentLeftWidthChanged += delegate { App.Ui.MediaItemsViewModel.ThumbsGridReloadItems(); };

      BindingOperations.SetBinding(TreeViewCategories.BtnPinPanel, ToggleButton.IsCheckedProperty,
        new Binding(nameof(SlidePanel.IsPinned)) { Source = SlidePanelMainTreeView });

      StatusPanel.SizeChanged += delegate {
        SlidePanelMainTreeView.BorderMargin = new(0, 0, 0, StatusPanel.ActualHeight);
        FullMedia.ClipsPanel.BorderMargin = new(0, 0, 0, StatusPanel.ActualHeight);
      };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      CommandsController.AddCommandBindings(CommandBindings);
      CommandsController.AddInputBindings();
      App.Core.WindowsDisplayScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;
      MenuViewers.Header = App.Core.CurrentViewer?.Title ?? "Viewer";
      App.Ui.MediaItemsViewModel.RegisterEvents();
    }

    public void SetMediaItemSource(MediaItem mediaItem, bool decoded = false) {
      switch (mediaItem.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(mediaItem, decoded);
          App.Ui.MediaItemClipsCategory.SetMediaItem(null);
          FullMedia.SetSource(null);
          break;
        }
        case MediaType.Video: {
          App.Ui.MediaItemClipsCategory.SetMediaItem(mediaItem);
          FullMedia.SetSource(mediaItem);
          break;
        }
      }
    }

    private void PanelFullScreen_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2)
        WindowCommands.SwitchToBrowser();
    }

    private void PanelFullScreen_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (MediaItemsCommands.CanNext())
          MediaItemsCommands.Next();
      }
      else {
        if (MediaItemsCommands.CanPrevious())
          MediaItemsCommands.Previous();
      }
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.MediaItems.ModifiedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modified, do you want to save them?", true)) {
        MetadataCommands.Save();
      }
      App.Db.SaveAllTables();
    }

    private void WMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      if (App.Ui.AppInfo.AppMode == AppMode.Viewer) return;
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private void FiltersPanel_ClearFilters(object sender, MouseButtonEventArgs e) {
      App.Ui.ClearFilters();
    }

    private void OnMediaTypesChanged(object sender, RoutedEventArgs e) {
      App.Ui.MediaItemsViewModel.ReapplyFilter();
    }
  }
}
