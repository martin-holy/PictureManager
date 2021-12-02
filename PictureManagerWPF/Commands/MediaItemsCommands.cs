using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.UserControls;

namespace PictureManager.Commands {
  public static class MediaItemsCommands {
    public static RoutedUICommand RebuildThumbnailsCommand { get; } = new() { Text = "Rebuild Thumbnails" };
    public static RoutedUICommand CompareCommand { get; } = new() { Text = "Compare" };
    public static RoutedUICommand SegmentMatchingCommand { get; } = new() { Text = "Segment Matching" };
    public static RoutedUICommand ViewMediaItemsWithSegmentCommand { get; } = new();

    private static ThumbnailsGridM ThumbsGrid => App.Core.ThumbnailsGridsM.Current;

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, RebuildThumbnailsCommand, RebuildThumbnails, CanRebuildThumbnails);
      CommandsController.AddCommandBinding(cbc, CompareCommand, Compare, CanCompare);
      CommandsController.AddCommandBinding(cbc, SegmentMatchingCommand, SegmentMatching, CanSegmentMatching);
      CommandsController.AddCommandBinding(cbc, ViewMediaItemsWithSegmentCommand, ViewMediaItemsWithSegment);
    }

    private static bool CanCompare() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void Compare() {
      App.WMain.ImageComparerTool.Visibility = Visibility.Visible;
      App.WMain.UpdateLayout();
      App.WMain.ImageComparerTool.SelectDefaultMethod();
      _ = App.WMain.ImageComparerTool.Compare();
    }

    public static bool CanRebuildThumbnails(object parameter) => parameter is FolderM || ThumbsGrid?.FilteredItems.Count > 0;

    public static void RebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mediaItems = parameter switch {
        FolderM folder => folder.GetMediaItems(recursive),
        List<MediaItemM> items => items,
        _ => ThumbsGrid.GetSelectedOrAll(),
      };

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Rebuilding thumbnails ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        async (mi) => {
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0, Settings.Default.JpegQualityLevel);
          mi.ReloadThumbnail();
        },
        mi => mi.FilePath,
        delegate {
          _ = App.Ui.ThumbnailsGridsVM.ThumbsGridReloadItems();
        });

      progress.Start();
    }

    private static bool CanSegmentMatching() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void SegmentMatching() {
      var mediaItems = ThumbsGrid.GetSelectedOrAll();
      var control = App.Ui.MainTabsVM.ActivateTab<SegmentMatchingControl>();
      var all = MessageDialog.Show("Segment Matching", "Do you want to load all segments or just segments with person?",
        true, new string[] { "All segments", "Segments with person" });

      control?.SetMediaItems(mediaItems);
      _ = control?.LoadSegmentsAsync(!all);
    }

    private static void ViewMediaItemsWithSegment(object parameter) {
      if (parameter is not Segment segment || segment.MediaItem == null) return;
      App.Core.MediaItemsM.Current = segment.MediaItem;
      WindowCommands.SwitchToFullScreen();

      List<MediaItemM> items = null;

      if (segment.PersonId == 0) {
        if (App.Ui.MainTabsVM.Selected is SegmentMatchingControl
          && App.Core.Segments.LoadedGroupedByPerson.Count > 0
          && App.Core.Segments.LoadedGroupedByPerson[^1].Any(x => x.PersonId == 0)) {
          items = App.Core.Segments.LoadedGroupedByPerson[^1].Select(x => x.MediaItem).Distinct().ToList();
        }
        else
          items = new List<MediaItemM> { segment.MediaItem };
      }
      else {
        items = App.Core.Segments.All.Cast<Segment>().Where(x => x.PersonId == segment.PersonId).Select(x => x.MediaItem).Distinct().OrderBy(x => x.FileName).ToList();
      }

      var itemsVM = App.Ui.MediaItemsBaseVM.ToViewModel(items).ToList();

      App.WMain.MediaViewer.SetMediaItems(itemsVM);
      App.WMain.MediaViewer.SetMediaItemSource(App.Ui.MediaItemsBaseVM.ToViewModel(segment.MediaItem));
    }
  }
}
