using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Patterns;
using PictureManager.Utils;
using PictureManager.ViewModels;

namespace PictureManager.Commands {
  public class MediaItemsCommands: Singleton<MediaItemsCommands> {
    public static RoutedUICommand NextCommand { get; } = CommandsController.CreateCommand("Next", "Next", new KeyGesture(Key.Right));
    public static RoutedUICommand PreviousCommand { get; } = CommandsController.CreateCommand("Previous", "Previous", new KeyGesture(Key.Left));
    public static RoutedUICommand SelectAllCommand { get; } = CommandsController.CreateCommand("Select All", "SelectAll", new KeyGesture(Key.A, ModifierKeys.Control));
    public static RoutedUICommand SelectNotModifiedCommand { get; } = new RoutedUICommand { Text = "Select Not Modified" };
    public static RoutedUICommand DeleteCommand { get; } = CommandsController.CreateCommand("Delete", "Delete", new KeyGesture(Key.Delete));
    public static RoutedUICommand PresentationCommand { get; } = CommandsController.CreateCommand("Presentation", "Presentation", new KeyGesture(Key.P, ModifierKeys.Control));
    public static RoutedUICommand CompressCommand { get; } = new RoutedUICommand { Text = "Compress" };
    public static RoutedUICommand RotateCommand { get; } = CommandsController.CreateCommand("Rotate", "Rotate", new KeyGesture(Key.R, ModifierKeys.Control));
    public static RoutedUICommand RebuildThumbnailsCommand { get; } = new RoutedUICommand { Text = "Rebuild Thumbnails" };
    public static RoutedUICommand ShuffleCommand { get; } = new RoutedUICommand { Text = "Shuffle" };
    public static RoutedUICommand ResizeImagesCommand { get; } = new RoutedUICommand { Text = "Resize Images" };
    public static RoutedUICommand CopyPathsCommand { get; } = new RoutedUICommand { Text = "Copy Paths" };
    public static RoutedUICommand CompareCommand { get; } = new RoutedUICommand { Text = "Compare" };
    public static RoutedUICommand ImagesToVideoCommand { get; } = new RoutedUICommand { Text = "Images to Video" };
    public static RoutedUICommand RenameCommand { get; } = CommandsController.CreateCommand("Rename", "Rename", new KeyGesture(Key.F2));
    public static RoutedUICommand VideoClipSplitCommand { get; } = CommandsController.CreateCommand("Split", "Split", new KeyGesture(Key.S, ModifierKeys.Alt));
    public static RoutedUICommand VideoClipsSaveCommand { get; } = new RoutedUICommand { Text = "Save Video Clips" };

    public void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, NextCommand, Next, CanNext);
      CommandsController.AddCommandBinding(cbc, PreviousCommand, Previous, CanPrevious);
      CommandsController.AddCommandBinding(cbc, SelectAllCommand, SelectAll, CanSelectAll);
      CommandsController.AddCommandBinding(cbc, SelectNotModifiedCommand, SelectNotModified, CanSelectNotModified);
      CommandsController.AddCommandBinding(cbc, DeleteCommand, Delete, CanDelete);
      CommandsController.AddCommandBinding(cbc, PresentationCommand, Presentation, CanPresentation);
      CommandsController.AddCommandBinding(cbc, CompressCommand, Compress, CanCompress);
      CommandsController.AddCommandBinding(cbc, RotateCommand, Rotate, CanRotate);
      CommandsController.AddCommandBinding(cbc, RebuildThumbnailsCommand, RebuildThumbnails, CanRebuildThumbnails);
      CommandsController.AddCommandBinding(cbc, ShuffleCommand, Shuffle, CanShuffle);
      CommandsController.AddCommandBinding(cbc, ResizeImagesCommand, ResizeImages, CanResizeImages);
      CommandsController.AddCommandBinding(cbc, ImagesToVideoCommand, ImagesToVideo, CanImagesToVideo);
      CommandsController.AddCommandBinding(cbc, CopyPathsCommand, CopyPaths, CanCopyPaths);
      CommandsController.AddCommandBinding(cbc, CompareCommand, Compare, CanCompare);
      CommandsController.AddCommandBinding(cbc, RenameCommand, Rename, CanRename);
      CommandsController.AddCommandBinding(cbc, VideoClipSplitCommand, VideoClipSplit, VideoSourceIsNotNull);
      CommandsController.AddCommandBinding(cbc, VideoClipsSaveCommand, VideoClipsSave, CanVideoClipsSave);
    }

    public static bool CanNext() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.Model.MediaItems.ThumbsGrid.GetNext() != null;
    }

    public void Next() {
      var current = App.Core.Model.MediaItems.ThumbsGrid.GetNext();
      App.Core.Model.MediaItems.ThumbsGrid.Current = current;
      var decoded = App.WMain.PresentationPanel.IsRunning && current.MediaType == MediaType.Image && current.IsPanoramic;
      App.WMain.SetMediaItemSource(decoded);

      if (App.WMain.PresentationPanel.IsRunning && (
            current.MediaType == MediaType.Video ||
            (current.IsPanoramic && App.WMain.PresentationPanel.PlayPanoramicImages))) {

        App.WMain.PresentationPanel.Pause();

        if (current.MediaType == MediaType.Image && current.IsPanoramic)
          App.WMain.PresentationPanel.Start(true);
      }

      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    public static bool CanPrevious() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.Model.MediaItems.ThumbsGrid.GetPrevious() != null;
    }

    public void Previous() {
      if (App.WMain.PresentationPanel.IsRunning)
        App.WMain.PresentationPanel.Stop();

      App.Core.Model.MediaItems.ThumbsGrid.Current = App.Core.Model.MediaItems.ThumbsGrid.GetPrevious();
      App.WMain.SetMediaItemSource();
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private static bool CanSelectAll() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void SelectAll() {
      App.Core.Model.MediaItems.ThumbsGrid.SelectAll();
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private static bool CanSelectNotModified() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void SelectNotModified() {
      App.Core.Model.MediaItems.ThumbsGrid.SelectNotModified();
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private static bool CanDelete() {
      return App.Core.Model.MediaItems.ThumbsGrid.Selected > 0;
    }

    private void Delete() {
      var items = App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList();
      var count = items.Count;
      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      App.Core.Model.MediaItems.ThumbsGrid.Remove(items,true, AppCore.FileOperationDelete);
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems();

      if (App.Core.AppInfo.AppMode == AppMode.Viewer) {
        if (App.Core.Model.MediaItems.ThumbsGrid.Current != null)
          App.WMain.SetMediaItemSource();
        else
          App.WMain.CommandsController.WindowCommands.SwitchToBrowser();
      }
    }

    private static bool CanShuffle() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void Shuffle() {
      App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Shuffle();
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems(false);
    }

    private static bool CanResizeImages() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void ResizeImages() {
      ResizeImagesDialog.Show(App.WMain, App.Core.Model.MediaItems.ThumbsGrid.GetSelectedOrAll());
    }

    private static bool CanImagesToVideo() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count(x => x.IsSelected && x.MediaType == MediaType.Image) > 0;
    }

    private static void ImagesToVideo() {
      ImagesToVideoDialog.ShowDialog(App.WMain,
        App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image), 
        async delegate(Folder folder, string fileName) {
          var mmi = App.Core.Model.MediaItems;

          // create new MediaItem, Read Metadata and Create Thumbnail
          var mi = new MediaItem(mmi.Helper.GetNextId(), folder, fileName);
          mmi.AddRecord(mi);
          folder.MediaItems.Add(mi);
          MediaItemsViewModel.ReadMetadata(mi);
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0);

          // reload grid
          mmi.ThumbsGrid.LoadedItems.AddInOrder(mi, (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase) >= 0);
          App.Core.MediaItemsViewModel.ReapplyFilter();
          App.Core.MediaItemsViewModel.ScrollTo(mi);
        }
      );
    }

    private static bool CanCopyPaths() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count(x => x.IsSelected) > 0;
    }

    private static void CopyPaths() {
      Clipboard.SetText(
        string.Join("\n",
          App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(x => x.FilePath)));
    }

    private static bool CanCompare() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void Compare() {
      App.WMain.ImageComparerTool.Visibility = Visibility.Visible;
      App.WMain.UpdateLayout();
      App.WMain.ImageComparerTool.SelectDefaultMethod();
      App.WMain.ImageComparerTool.Compare();
    }

    private static bool CanCompress() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private void Compress() {
      CompressDialog.ShowDialog(App.WMain);
    }

    private static bool CanRotate() {
      return App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count(x => x.IsSelected) > 0;
    }

    private void Rotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      App.Core.MediaItemsViewModel.SetOrientation(App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToArray(), rotation);

      if (App.Core.AppInfo.AppMode != AppMode.Viewer) return;
      App.WMain.SetMediaItemSource();
    }

    public bool CanRebuildThumbnails(object parameter) {
      return parameter is Folder || App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    public void RebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      List<MediaItem> mediaItems;

      switch (parameter) {
        case Folder folder: mediaItems = folder.GetMediaItems(recursive); break;
        case List<MediaItem> items: mediaItems = items; break;
        default: mediaItems = App.Core.Model.MediaItems.ThumbsGrid.GetSelectedOrAll(); break;
      }

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Rebuilding thumbnails ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        async delegate (MediaItem mi) {
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0);
          mi.ReloadThumbnail();
        },
        mi => mi.FilePath,
        delegate {
          App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
        });

      progress.Start();
    }

    public void Resize(MediaItem[] items, int px, string destination, bool withMetadata, bool withThumbnail) {
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
            Imaging.ResizeJpg(src, dest, px, withMetadata, withThumbnail);
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

    private static bool CanPresentation() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.Model.MediaItems.ThumbsGrid.Current != null;
    }

    private void Presentation() {
      if (App.WMain.FullImage.IsAnimationOn) {
        App.WMain.FullImage.Stop();
        App.WMain.PresentationPanel.Stop();
        return;
      }

      if (App.WMain.PresentationPanel.IsRunning || App.WMain.PresentationPanel.IsPaused)
        App.WMain.PresentationPanel.Stop();
      else
        App.WMain.PresentationPanel.Start(true);
    }

    private static bool CanRename() {
      return App.Core.Model.MediaItems.ThumbsGrid.Current != null;
    }

    private static void Rename() {
      var current = App.Core.Model.MediaItems.ThumbsGrid.Current;
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.Notification,
        Title = "Rename",
        Question = "Add a new name.",
        Answer = Path.GetFileNameWithoutExtension(current.FileName)
      };

      inputDialog.BtnDialogOk.Click += delegate {
        var newFileName = inputDialog.TxtAnswer.Text + Path.GetExtension(current.FileName);

        if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) > 0)) {
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
        App.Core.Model.MediaItems.ThumbsGrid.FilteredItemsSetInPlace(current);
        App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
        App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.FilePath));
        App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.DateAndTime));
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
      }
    }

    private static bool VideoSourceIsNotNull() {
      return App.WMain.FullMedia.Player.Source != null;
    }

    private static void VideoClipSplit() {
      var vc = App.WMain.FullMedia.CurrentVideoClip;
      if (vc != null && vc.Clip.TimeEnd == 0)
        App.WMain.FullMedia.SetMarker(vc, false);
      else
        App.Core.MediaItemClipsCategory.ItemCreate(App.Core.MediaItemClipsCategory, string.Empty);
    }

    private static bool CanVideoClipsSave() {
      return App.Core.Model.VideoClips.Helper.IsModified || App.Core.Model.VideoClipsGroups.Helper.IsModified;
    }

    private static void VideoClipsSave() {
      App.Core.Model.VideoClips.SaveToFile();
      App.Core.Model.VideoClipsGroups.SaveToFile();
    }
  }
}
