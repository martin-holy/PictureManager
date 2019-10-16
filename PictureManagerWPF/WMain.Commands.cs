using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PictureManager.Dialogs;
using PictureManager.Properties;
using PictureManager.ViewModel;
using PictureManager.Database;

namespace PictureManager {
  public partial class WMain {

    private void AddCommandBindings() {
      // Window Commands
      Commands.AddCommandBinding(CommandBindings, Commands.SwitchToFullScreen, SwitchToFullScreen, CanSwitchToFullScreen);
      Commands.AddCommandBinding(CommandBindings, Commands.SwitchToBrowser, SwitchToBrowser, CanSwitchToBrowser);
      
      // MediaItems Commands
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemNext, MediaItemNext, CanMediaItemNext);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemPrevious, MediaItemPrevious, CanMediaItemPrevious);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsSelectAll, MediaItemsSelectAll, CanMediaItemsSelectAll);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsSelectNotModifed, MediaItemsSelectNotModifed, CanMediaItemsSelectNotModifed);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsDelete, MediaItemsDelete, CanMediaItemsDelete);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsLoadByTag, MediaItemsLoadByTag);
      Commands.AddCommandBinding(CommandBindings, Commands.Presentation, Presentation, CanPresentation);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsCompress, MediaItemsCompress, CanMediaItemsCompress);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsRotate, MediaItemsRotate, CanMediaItemsRotate);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsRebuildThumbnails, MediaItemsRebuildThumbnails, CanMediaItemsRebuildThumbnails);
      Commands.AddCommandBinding(CommandBindings, Commands.MediaItemsShuffle, MediaItemsShuffle, CanMediaItemsShuffle);
      
      // TreeView Commands
      Commands.AddCommandBinding(CommandBindings, Commands.CategoryGroupNew, CategoryGroupNew);
      Commands.AddCommandBinding(CommandBindings, Commands.CategoryGroupRename, CategoryGroupRename);
      Commands.AddCommandBinding(CommandBindings, Commands.CategoryGroupDelete, CategoryGroupDelete);
      Commands.AddCommandBinding(CommandBindings, Commands.TagItemNew, TagItemNew);
      Commands.AddCommandBinding(CommandBindings, Commands.TagItemRename, TagItemRename);
      Commands.AddCommandBinding(CommandBindings, Commands.TagItemDelete, TagItemDelete);
      Commands.AddCommandBinding(CommandBindings, Commands.TagItemDeleteNotUsed, TagItemDeleteNotUsed);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderNew, FolderNew);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderRename, FolderRename);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderDelete, FolderDelete);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderAddToFavorites, FolderAddToFavorites);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderRemoveFromFavorites, FolderRemoveFromFavorites);
      Commands.AddCommandBinding(CommandBindings, Commands.FolderSetAsFolderKeyword, FolderSetAsFolderKeyword);
      Commands.AddCommandBinding(CommandBindings, Commands.ViewerIncludeFolder, ViewerIncludeFolder);
      Commands.AddCommandBinding(CommandBindings, Commands.ViewerExcludeFolder, ViewerExcludeFolder);
      Commands.AddCommandBinding(CommandBindings, Commands.ViewerRemoveFolder, ViewerRemoveFolder);
      Commands.AddCommandBinding(CommandBindings, Commands.GeoNameNew, GeoNameNew);
      
      // Metadata Commands
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataEdit, MetadataEdit, CanMetadataEdit);
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataSave, MetadataSave, CanMetadataSave);
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataCancel, MetadataCancel, CanMetadataCancel);
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataComment, MetadataComment, CanMetadataComment);
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataReload, MetadataReload, CanMetadataReload);
      Commands.AddCommandBinding(CommandBindings, Commands.MetadataReload2, MetadataReload, CanMetadataReload);
      
      Commands.AddCommandBinding(CommandBindings, Commands.TestButton, TestButton);
      Commands.AddCommandBinding(CommandBindings, Commands.OpenSettings, OpenSettings);
      Commands.AddCommandBinding(CommandBindings, Commands.AddGeoNamesFromFiles, AddGeoNamesFromFiles, CanAddGeoNamesFromFiles);
      Commands.AddCommandBinding(CommandBindings, Commands.ViewerChange, ViewerChange);
      Commands.AddCommandBinding(CommandBindings, Commands.OpenAbout, OpenAbout);
      Commands.AddCommandBinding(CommandBindings, Commands.OpenFolderKeywordsList, OpenFolderKeywordsList);
      Commands.AddCommandBinding(CommandBindings, Commands.ShowHideTabMain, ShowHideTabMain);
      Commands.AddCommandBinding(CommandBindings, Commands.OpenLog, OpenLog);
    }

    private void AddInputBindings() {
      MediaCommands.TogglePlayPause.InputGestures.Add(new KeyGesture(Key.Space));
      MediaCommands.TogglePlayPause.InputGestures.Add(new MouseGesture(MouseAction.LeftClick));

      SetTargetToCommand(MediaCommands.TogglePlayPause, FullMedia);
      SetTargetToCommand(Commands.MediaItemsSelectAll, ThumbsBox);
    }

    private void SetTargetToCommand(RoutedCommand command, IInputElement commandTarget) {
      foreach (InputGesture ig in command.InputGestures)
        InputBindings.Add(new InputBinding(command, ig) {CommandTarget = commandTarget});
    }

    private static bool CanMediaItemNext() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.GetNext() != null;
    }

    private void MediaItemNext() {
      var current = App.Core.MediaItems.GetNext();
      App.Core.MediaItems.Current = current;
      SetMediaItemSource();
      if (_presentationTimer.Enabled && (current.MediaType == MediaType.Video || current.IsPanoramatic)) {
        _presentationTimer.Enabled = false;
        _presentationTimerPaused = true;

        if (current.MediaType == MediaType.Image && current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
      }

      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemPrevious() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.GetPrevious() != null;
    }

    private void MediaItemPrevious() {
      App.Core.MediaItems.Current = App.Core.MediaItems.GetPrevious();
      SetMediaItemSource();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsSelectAll() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private static void MediaItemsSelectAll() {
      App.Core.MediaItems.SelectAll();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsSelectNotModifed() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private static void MediaItemsSelectNotModifed() {
      App.Core.MediaItems.SelectNotModifed();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsDelete() {
      return App.Core.MediaItems.Selected > 0;
    }

    private void MediaItemsDelete() {
      var count = App.Core.MediaItems.FilteredItems.Count(x => x.IsSelected);
      if (!MessageDialog.Show("Delete Confirmation", 
        $"Do you realy want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      App.Core.MediaItems.RemoveSelected(true);

      if (App.Core.AppInfo.AppMode == AppMode.Viewer) {
        if (App.Core.MediaItems.Current != null)
          SetMediaItemSource();
        else
          SwitchToBrowser();
      }
    }

    private static bool CanMediaItemsShuffle() {
      return App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private static void MediaItemsShuffle() {
      App.Core.MediaItems.FilteredItems.Shuffle();
      App.Core.MediaItems.SplitedItemsReload();
    }

    private static void MediaItemsLoadByTag(object parameter) {
      App.Core.MediaItems.LoadByTag((BaseTreeViewTagItem) parameter, (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private static bool CanPresentation() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.Current != null;
    }

    private void Presentation() {
      if (FullImage.IsAnimationOn) {
        FullImage.Stop();
        return;
      }
      
      if (_presentationTimer.Enabled)
        _presentationTimer.Enabled = false;
      else {
        if (App.Core.MediaItems.Current.MediaType == MediaType.Image && App.Core.MediaItems.Current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
        else
          StartPresentationTimer(true);
      }
    }

    private static void CategoryGroupNew(object parameter) {
      (parameter as BaseCategoryItem)?.GroupNewOrRename(null, false);
    }

    private static void CategoryGroupRename(object parameter) {
      var group = parameter as CategoryGroup;
      (group?.Parent as BaseCategoryItem)?.GroupNewOrRename(group, true);
    }

    private static void CategoryGroupDelete(object parameter) {
      var group = parameter as CategoryGroup;
      (group?.Parent as BaseCategoryItem)?.GroupDelete(group);
    }

    private static void TagItemNew(object parameter) {
      var item = parameter as BaseTreeViewItem;
      (item?.GetTopParent() as BaseCategoryItem)?.ItemNewOrRename(item, false);
    }

    private static void TagItemRename(object parameter) {
      var item = parameter as BaseTreeViewItem;
      (item?.GetTopParent() as BaseCategoryItem)?.ItemNewOrRename(item, true);
    }

    private static void TagItemDelete(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you realy want to delete '{item.Title}'?", true)) return;
      (item.GetTopParent() as BaseCategoryItem)?.ItemDelete(item);
    }

    private static void TagItemDeleteNotUsed(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!(item.GetTopParent() is BaseCategoryItem topParent)) return;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you realy want to delete not used items in '{item.Title}'?", true)) return;

      switch (topParent.Category) {
        case Category.People: {
          foreach (var person in item.Items.Cast<Person>().Where(x => x.MediaItems.Count == 0).ToArray())
            topParent.ItemDelete(person);

          break;
        }
        case Category.Keywords: {
          foreach (var keyword in item.Items.Cast<Keyword>().Where(x => x.MediaItems.Count == 0).ToArray())
            topParent.ItemDelete(keyword);

          break;
        }
      }
    }

    private static void ViewerIncludeFolder(object parameter) {
      ((Viewer) parameter).AddFolder(true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ((Viewer) parameter).AddFolder(false);
    }

    private static void ViewerRemoveFolder(object parameter) {
      var folder = (BaseTreeViewItem) parameter;
      folder.Parent?.Items.Remove(folder);
      App.Core.Viewers.Helper.Table.SaveToFile();
    }

    private static void FolderNew(object parameter) {
      ((Folder) parameter).NewOrRename(false);
    }

    private static void FolderRename(object parameter) {
      ((Folder) parameter).NewOrRename(true);
    }

    private static void FolderDelete(object parameter) {
      var folder = (Folder) parameter;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you realy want to delete '{folder.Title}' folder?", true)) return;

      App.Core.Folders.DeleteRecord(folder, true);
      // reload FolderKeywords
      App.Core.FolderKeywords.Load();
    }

    private static void FolderAddToFavorites(object parameter) {
      App.Core.FavoriteFolders.Add((Folder) parameter);
    }

    private static void FolderRemoveFromFavorites(object parameter) {
      App.Core.FavoriteFolders.Remove((FavoriteFolder) parameter);
    }

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder) parameter).IsFolderKeyword = true;
      App.Core.Folders.Helper.Table.SaveToFile();
      App.Core.FolderKeywords.Load();
    }

    private void GeoNameNew(object parameter) {
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = IconName.LocationCheckin,
        Title = "GeoName latitude and longitude",
        Question = "Enter in format: N36.75847,W3.84609",
        Answer = ""
      };

      inputDialog.BtnDialogOk.Click += delegate { inputDialog.DialogResult = true; };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        ((GeoNames) parameter).New(inputDialog.Answer);
      }
    }

    private static bool CanMediaItemsCompress() {
      return App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private void MediaItemsCompress() {
      CompressDialog.ShowDialog(this);
    }

    private static bool CanMediaItemsRotate() {
      return App.Core.MediaItems.FilteredItems.Count(x => x.IsSelected) > 0;
    }

    private void MediaItemsRotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      App.Core.MediaItems.SetOrientation(App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).ToArray(), rotation);

      if (App.Core.AppInfo.AppMode != AppMode.Viewer) return;
      SetMediaItemSource();
    }

    private void OpenSettings() {
      var settings = new WSettings {Owner = this};
      if (settings.ShowDialog() ?? true)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private void OpenAbout() {
      var about = new WAbout {Owner = this};
      about.ShowDialog();
    }

    private void ShowHideTabMain(object parameter) {
      var show = false;
      var reload = false;
      if (parameter != null)
        show = (bool) parameter;
      else {
        switch (App.Core.AppInfo.AppMode) {
          case AppMode.Browser:
            reload = true;
            _mainTreeViewIsPinnedInBrowser = !_mainTreeViewIsPinnedInBrowser;
            show = _mainTreeViewIsPinnedInBrowser;
            break;
          case AppMode.Viewer:
            _mainTreeViewIsPinnedInViewer = !_mainTreeViewIsPinnedInViewer;
            show = _mainTreeViewIsPinnedInViewer;
            break;
        }
      }

      if (show) {
        GridMain.ColumnDefinitions[0].Width = new GridLength(FlyoutMainTreeView.ActualWidth);
        GridMain.ColumnDefinitions[1].Width = new GridLength(3);
      }
      else {
        GridMain.ColumnDefinitions[0].Width = new GridLength(0);
        GridMain.ColumnDefinitions[1].Width = new GridLength(0);
      }

      FlyoutMainTreeView.IsPinned = show;
      FlyoutMainTreeView.IsOpen = show;

      if (show) {
        FlyoutStatusPanel.IsOpen = false;
        FlyoutStatusPanel.IsOpen = true;
      }

      if (reload) {
        App.Core.MediaItems.SplitedItemsReload();
        App.Core.MediaItems.ScrollToCurrent();
      }
    }

    private void OpenFolderKeywordsList() {
      var fkl = new FolderKeywordList {Owner = this};
      fkl.ShowDialog();
    }

    private static bool CanMetadataEdit() {
      return !App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private void MetadataEdit() {
      Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)] = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      App.Core.MediaItems.IsEditModeOn = true;
    }

    private static bool CanMetadataSave() {
      return App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.ModifedItems.Count > 0;
    }

    private void MetadataSave() {
      var progress = new ProgressBarDialog(this, true, Environment.ProcessorCount, "Saving metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifedItems.ToArray(),
        null,
        // action
        delegate(MediaItem mi) {
          mi.TryWriteMetadata();

          Application.Current.Dispatcher.Invoke(delegate {
            App.Core.MediaItems.SetModifed(mi, false);
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate (object sender, RunWorkerCompletedEventArgs e) {
          if (e.Cancelled) {
            MetadataCancel();
          }
          else {
            if ((bool) Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)])
              TabFolders.IsSelected = true;

            App.Core.MediaItems.IsEditModeOn = false;
          }
        });

      progress.StartDialog();
    }

    private static bool CanMetadataCancel() {
      return App.Core.MediaItems.IsEditModeOn;
    }

    private void MetadataCancel() {
      var progress = new ProgressBarDialog(this, false, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifedItems.ToArray(),
        null,
        // action
        delegate(MediaItem mi) {
          mi.ReadMetadata();

          Application.Current.Dispatcher.Invoke(delegate {
            App.Core.MediaItems.SetModifed(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Sdb.SaveAllTables();
          App.Core.MarkUsedKeywordsAndPeople();
          App.Core.MediaItems.IsEditModeOn = false;
          if ((bool) Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)])
            TabFolders.IsSelected = true;
        });

      progress.StartDialog();
    }

    private static bool CanMetadataComment() {
      return App.Core.MediaItems.Current != null;
    }

    private void MetadataComment() {
      var current = App.Core.MediaItems.Current;
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = IconName.Notification,
        Title = "Comment",
        Question = "Add a comment.",
        Answer = current.Comment
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (inputDialog.TxtAnswer.Text.Length > 256) {
          inputDialog.ShowErrorMessage("Comment is too long!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      current.Comment = MediaItems.NormalizeComment(inputDialog.TxtAnswer.Text);
      current.TryWriteMetadata();
      current.SetInfoBox();
      current.OnPropertyChanged(nameof(current.Comment));
      App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.IsCommentVisible));
    }

    private static bool CanMetadataReload(object parameter) {
      return parameter is Folder || App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private void MetadataReload(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var folder = parameter as Folder;
      var mediaItems = folder != null
        ? folder.GetMediaItems(recursive)
        : App.Core.MediaItems.GetSelectedOrAll();

      var progress = new ProgressBarDialog(this, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        delegate(MediaItem mi) {
          mi.ReadMetadata();

          // set info box just for loaded media items
          if (folder == null)
            Application.Current.Dispatcher.Invoke(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.MediaItems.Helper.IsModifed = true;
          App.Core.Sdb.SaveAllTables();
        });

      progress.Start();
    }

    public bool CanMediaItemsRebuildThumbnails(object parameter) {
      return parameter is Folder || App.Core.MediaItems.FilteredItems.Count > 0;
    }

    public void MediaItemsRebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      List<MediaItem> mediaItems;

      switch (parameter) {
        case Folder folder: mediaItems = folder.GetMediaItems(recursive); break;
        case List<MediaItem> items: mediaItems = items; break;
        default: mediaItems = App.Core.MediaItems.GetSelectedOrAll(); break;
      }

      var progress = new ProgressBarDialog(this, true, Environment.ProcessorCount, "Rebuilding thumbnails ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        delegate(MediaItem mi) {
          mi.SetThumbSize();
          App.Core.CreateThumbnail(mi);
        },
        mi => mi.FilePath,
        delegate {
          App.Core.MediaItems.SplitedItemsReload();
          App.Core.MediaItems.ScrollToCurrent();
        });

      progress.Start();
    }

    public void MediaItemsResize(MediaItem[] items, int px, string destination, bool withMetadata, bool withThumbnail) {
      var progress = new ProgressBarDialog(this, true, Environment.ProcessorCount, "Resizing Images ...");

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
            MediaItems.Resize(src, dest, px, withMetadata, withThumbnail);
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

    private static bool CanAddGeoNamesFromFiles() {
      return App.Core.MediaItems.FilteredItems.Count(x => x.IsSelected) > 0;
    }

    private void AddGeoNamesFromFiles() {
      var progress = new ProgressBarDialog(this, true, 1, "Adding geonames ...");
      progress.AddEvents(
        App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).ToArray(),
        null,
        // action
        delegate(MediaItem mi) {
          if (mi.Lat == null || mi.Lng == null) mi.ReadMetadata(true);
          if (mi.Lat == null || mi.Lng == null) return;

          var lastGeoName = App.Core.GeoNames.InsertGeoNameHierarchy((double) mi.Lat, (double) mi.Lng);
          if (lastGeoName == null) return;

          mi.GeoName = lastGeoName;
          mi.TryWriteMetadata();
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Sdb.SaveAllTables();
        });

      progress.StartDialog();
    }

    private void ViewerChange(object parameter) {
      if (App.Core.CurrentViewer != null)
        App.Core.CurrentViewer.IsDefault = false;

      var viewer = (Viewer) parameter;
      viewer.IsDefault = true;
      App.Core.Viewers.Helper.Table.SaveToFile();

      MenuViewers.Header = viewer.Title;
      App.Core.CurrentViewer = viewer;
      App.Core.Folders.AddDrives();
      App.Core.FolderKeywords.Load();
    }

    private static bool CanSwitchToFullScreen() {
      return App.Core.AppInfo.AppMode == AppMode.Browser;
    }

    private void SwitchToFullScreen() {
      if (App.Core.MediaItems.Current == null) return;
      App.Core.AppInfo.AppMode = AppMode.Viewer;
      ShowHideTabMain(_mainTreeViewIsPinnedInViewer);
      UseNoneWindowStyle = true;
      IgnoreTaskbarOnMaximize = true;
      MainMenu.Visibility = Visibility.Hidden;
    }

    private static bool CanSwitchToBrowser() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer;
    }

    private void SwitchToBrowser() {
      _presentationTimer.Enabled = false;
      App.Core.AppInfo.AppMode = AppMode.Browser;
      ShowHideTabMain(_mainTreeViewIsPinnedInBrowser);
      App.Core.MediaItems.SplitedItemsReload();
      App.Core.MediaItems.ScrollToCurrent();
      App.Core.MarkUsedKeywordsAndPeople();
      UseNoneWindowStyle = false;
      ShowTitleBar = true;
      IgnoreTaskbarOnMaximize = false;
      MainMenu.Visibility = Visibility.Visible;
      FullImage.SetSource(null);
      FullImage.Stop();
      FullMedia.MediaElement.Source = null;
      FullMedia.IsPlaying = false;
    }

    private void OpenLog() {
      var log = new LogDialog {Owner = this};
      log.ShowDialog();
    }
  }
}