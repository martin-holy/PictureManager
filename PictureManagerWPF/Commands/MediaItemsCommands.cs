using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.UserControls;
using PictureManager.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PictureManager.Commands {
  public static class MediaItemsCommands {
    public static RoutedUICommand SelectAllCommand { get; } = CommandsController.CreateCommand("Select All", "SelectAll", new KeyGesture(Key.A, ModifierKeys.Control));
    public static RoutedUICommand SelectNotModifiedCommand { get; } = new() { Text = "Select Not Modified" };
    public static RoutedUICommand DeleteCommand { get; } = CommandsController.CreateCommand("Delete", "Delete", new KeyGesture(Key.Delete));
    public static RoutedUICommand CompressCommand { get; } = new() { Text = "Compress" };
    public static RoutedUICommand RotateCommand { get; } = CommandsController.CreateCommand("Rotate", "Rotate", new KeyGesture(Key.R, ModifierKeys.Control));
    public static RoutedUICommand RebuildThumbnailsCommand { get; } = new() { Text = "Rebuild Thumbnails" };
    public static RoutedUICommand ShuffleCommand { get; } = new() { Text = "Shuffle" };
    public static RoutedUICommand ResizeImagesCommand { get; } = new() { Text = "Resize Images" };
    public static RoutedUICommand CopyPathsCommand { get; } = new() { Text = "Copy Paths" };
    public static RoutedUICommand CompareCommand { get; } = new() { Text = "Compare" };
    public static RoutedUICommand ImagesToVideoCommand { get; } = new() { Text = "Images to Video" };
    public static RoutedUICommand RenameCommand { get; } = CommandsController.CreateCommand("Rename", "Rename", new KeyGesture(Key.F2));
    public static RoutedUICommand SegmentMatchingCommand { get; } = new() { Text = "Segment Matching" };
    public static RoutedUICommand ViewMediaItemsWithSegmentCommand { get; } = new();

    private static ThumbnailsGrid ThumbsGrid => App.Core.MediaItems.ThumbsGrid;

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, SelectAllCommand, SelectAll, CanSelectAll);
      CommandsController.AddCommandBinding(cbc, SelectNotModifiedCommand, SelectNotModified, CanSelectNotModified);
      CommandsController.AddCommandBinding(cbc, DeleteCommand, Delete, CanDelete);
      CommandsController.AddCommandBinding(cbc, CompressCommand, Compress, CanCompress);
      CommandsController.AddCommandBinding(cbc, RotateCommand, Rotate, CanRotate);
      CommandsController.AddCommandBinding(cbc, RebuildThumbnailsCommand, RebuildThumbnails, CanRebuildThumbnails);
      CommandsController.AddCommandBinding(cbc, ShuffleCommand, Shuffle, CanShuffle);
      CommandsController.AddCommandBinding(cbc, ResizeImagesCommand, ResizeImages, CanResizeImages);
      CommandsController.AddCommandBinding(cbc, ImagesToVideoCommand, ImagesToVideo, CanImagesToVideo);
      CommandsController.AddCommandBinding(cbc, CopyPathsCommand, CopyPaths, CanCopyPaths);
      CommandsController.AddCommandBinding(cbc, CompareCommand, Compare, CanCompare);
      CommandsController.AddCommandBinding(cbc, RenameCommand, Rename, CanRename);
      CommandsController.AddCommandBinding(cbc, SegmentMatchingCommand, SegmentMatching, CanSegmentMatching);
      CommandsController.AddCommandBinding(cbc, ViewMediaItemsWithSegmentCommand, ViewMediaItemsWithSegment);
    }

    private static bool CanSelectAll() => App.Ui.AppInfo.AppMode == AppMode.Browser && ThumbsGrid?.FilteredItems.Count > 0;

    private static void SelectAll() {
      ThumbsGrid.SelectAll();
      App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.FileSize));
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanSelectNotModified() => App.Ui.AppInfo.AppMode == AppMode.Browser && ThumbsGrid?.FilteredItems.Count > 0;

    private static void SelectNotModified() {
      ThumbsGrid.SelectNotModified();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanDelete() => ThumbsGrid?.SelectedItems.Count > 0 || App.Ui.AppInfo.AppMode == AppMode.Viewer;

    private async static void Delete() {
      var items = App.Ui.AppInfo.AppMode == AppMode.Viewer
        ? new List<MediaItem>() { App.Core.MediaItems.Current }
        : ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList();
      var count = items.Count;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      var newCurrent = MediaItems.GetNewCurrent(ThumbsGrid != null
        ? ThumbsGrid.LoadedItems
        : App.WMain.MediaViewer.MediaItems,
        items);

      App.Core.MediaItems.Delete(items, AppCore.FileOperationDelete);
      App.Core.MediaItems.Current = newCurrent;
      await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();

      if (App.WMain.MainTabs.GetSelectedContent() is SegmentMatchingControl smc)
        _ = smc.SortAndReload();

      if (App.Ui.AppInfo.AppMode == AppMode.Viewer) {
        _ = App.WMain.MediaViewer.MediaItems.Remove(items[0]);
        if (App.Core.MediaItems.Current != null)
          App.WMain.MediaViewer.SetMediaItemSource(App.Core.MediaItems.Current);
        else
          WindowCommands.SwitchToBrowser();
      }
    }

    private static bool CanShuffle() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void Shuffle() {
      ThumbsGrid.FilteredItems.Shuffle();
      ThumbsGrid.GroupByFolders = false;
      ThumbsGrid.GroupByDate = false;
      _ = App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private static bool CanResizeImages() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void ResizeImages() => ResizeImagesDialog.Show(App.WMain, ThumbsGrid.GetSelectedOrAll());

    private static bool CanImagesToVideo() => ThumbsGrid?.FilteredItems.Count(x => x.IsSelected && x.MediaType == MediaType.Image) > 0;

    private static void ImagesToVideo() {
      ImagesToVideoDialog.ShowDialog(App.WMain,
        ThumbsGrid.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image),
        async (Folder folder, string fileName) => {
          var mmi = App.Core.MediaItems;

          // create new MediaItem, Read Metadata and Create Thumbnail
          var mi = new MediaItem(mmi.Helper.GetNextId(), folder, fileName);
          mmi.All.Add(mi);
          folder.MediaItems.Add(mi);
          await MediaItemsViewModel.ReadMetadata(mi);
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0, Settings.Default.JpegQualityLevel);

          // reload grid
          mmi.ThumbsGrid.LoadedItems.AddInOrder(mi,
            (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase) >= 0);
          await App.Ui.MediaItemsViewModel.ReapplyFilter();
          App.Ui.MediaItemsViewModel.ScrollTo(mi);
        }
      );
    }

    private static bool CanCopyPaths() => ThumbsGrid?.FilteredItems.Count(x => x.IsSelected) > 0;

    private static void CopyPaths() =>
      Clipboard.SetText(string.Join("\n", ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(x => x.FilePath)));

    private static bool CanCompare() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void Compare() {
      App.WMain.ImageComparerTool.Visibility = Visibility.Visible;
      App.WMain.UpdateLayout();
      App.WMain.ImageComparerTool.SelectDefaultMethod();
      _ = App.WMain.ImageComparerTool.Compare();
    }

    private static bool CanCompress() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void Compress() => CompressDialog.ShowDialog(App.WMain);

    private static bool CanRotate() => ThumbsGrid?.FilteredItems.Count(x => x.IsSelected) > 0;

    private static void Rotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      App.Ui.MediaItemsViewModel.SetOrientation(ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToArray(), rotation);

      if (App.Ui.AppInfo.AppMode != AppMode.Viewer) return;
      App.WMain.MediaViewer.SetMediaItemSource(ThumbsGrid.Current);
    }

    public static bool CanRebuildThumbnails(object parameter) => parameter is Folder || ThumbsGrid?.FilteredItems.Count > 0;

    public static void RebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mediaItems = parameter switch {
        Folder folder => folder.GetMediaItems(recursive),
        List<MediaItem> items => items,
        _ => ThumbsGrid.GetSelectedOrAll(),
      };

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Rebuilding thumbnails ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        async (MediaItem mi) => {
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0, Settings.Default.JpegQualityLevel);
          mi.ReloadThumbnail();
        },
        mi => mi.FilePath,
        delegate {
          _ = App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
        });

      progress.Start();
    }

    public static void Resize(MediaItem[] items, int px, string destination, bool withMetadata, bool withThumbnail) {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Resizing Images ...");

      progress.AddEvents(
        items,
        // doBeforeLoop
        delegate {
          try {
            Directory.CreateDirectory(destination);
            return true;
          }
          catch (Exception ex) {
            App.Core.LogError(ex, destination);
            return false;
          }
        },
        // action
        delegate (MediaItem mi) {
          if (mi.MediaType == MediaType.Video) return;

          try {
            var src = mi.FilePath;
            var dest = Path.Combine(destination, mi.FileName);
            Imaging.ResizeJpg(src, dest, px, withMetadata, withThumbnail, Settings.Default.JpegQualityLevel);
          }
          catch (Exception ex) {
            App.Core.LogError(ex, mi.FilePath);
          }
        },
        // customMessage
        mi => mi.FilePath,
        // onCompleted
        null);

      progress.Start();
    }

    private static bool CanRename() => App.Core.MediaItems.Current != null;

    private async static void Rename() {
      var current = App.Core.MediaItems.Current;
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.Notification,
        Title = "Rename",
        Question = "Add a new name.",
        Answer = Path.GetFileNameWithoutExtension(current.FileName)
      };

      inputDialog.BtnDialogOk.Click += delegate {
        var newFileName = inputDialog.TxtAnswer.Text + Path.GetExtension(current.FileName);

        if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) != -1)) {
          inputDialog.ShowErrorMessage("New file name contains invalid character!");
          return;
        }

        if (File.Exists(Domain.Extensions.PathCombine(current.Folder.FullPath, newFileName))) {
          inputDialog.ShowErrorMessage("New file name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;

      try {
        current.Rename(inputDialog.TxtAnswer.Text + Path.GetExtension(current.FileName));
        ThumbsGrid?.FilteredItemsSetInPlace(current);
        await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
        App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.FilePath));
        App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.DateAndTime));
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
      }
    }

    private static bool CanSegmentMatching() => ThumbsGrid?.FilteredItems.Count > 0;

    private static void SegmentMatching() {
      var mediaItems = ThumbsGrid.GetSelectedOrAll();
      var control = App.WMain.MainTabs.GetContentOfType<SegmentMatchingControl>();
      var all = MessageDialog.Show("Segment Matching", "Do you want to load all segments or just segments with person?",
        true, new string[] { "All segments", "Segments with person" });

      control?.SetMediaItems(mediaItems);
      _ = control?.LoadSegmentsAsync(!all);
    }

    private static void ViewMediaItemsWithSegment(object parameter) {
      if (parameter is not Segment segment) return;
      App.Core.MediaItems.Current = segment.MediaItem;
      WindowCommands.SwitchToFullScreen();

      List<MediaItem> items = null;

      if (segment.PersonId == 0) {
        if (App.WMain.MainTabs.GetSelectedContent() is SegmentMatchingControl
          && App.Core.Segments.LoadedGroupedByPerson.Count > 0
          && App.Core.Segments.LoadedGroupedByPerson[^1].Any(x => x.PersonId == 0)) {
          items = App.Core.Segments.LoadedGroupedByPerson[^1].Select(x => x.MediaItem).Distinct().ToList();
        }
        else
          items = new List<MediaItem> { segment.MediaItem };
      }
      else {
        items = App.Core.Segments.All.Cast<Segment>().Where(x => x.PersonId == segment.PersonId).Select(x => x.MediaItem).Distinct().OrderBy(x => x.FileName).ToList();
      }

      App.WMain.MediaViewer.SetMediaItems(items);
      App.WMain.MediaViewer.SetMediaItemSource(segment.MediaItem);
    }
  }
}
