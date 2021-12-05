using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;

namespace PictureManager.Commands {
  public static class MediaItemsCommands {
    public static RoutedUICommand RebuildThumbnailsCommand { get; } = new() { Text = "Rebuild Thumbnails" };
    public static RoutedUICommand CompareCommand { get; } = new() { Text = "Compare" };

    private static ThumbnailsGridM ThumbsGrid => App.Core.ThumbnailsGridsM.Current;

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, RebuildThumbnailsCommand, RebuildThumbnails, CanRebuildThumbnails);
      CommandsController.AddCommandBinding(cbc, CompareCommand, Compare, CanCompare);
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
  }
}
