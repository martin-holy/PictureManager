using PictureManager.Commands;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Models;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.UserControls;
using PictureManager.ViewModels;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PictureManager {
  public class AppCore : ILogger {
    public MediaItemsViewModel MediaItemsViewModel { get; }
    public MediaItemClipsCategory MediaItemClipsCategory { get; }
    public AppInfo AppInfo { get; } = new();
    public ObservableCollection<LogItem> Log { get; set; } = new();

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;
      App.Core.Logger = this;

      AppInfo.ProgressBarValueA = 100;
      AppInfo.ProgressBarValueB = 100;

      MediaItemsViewModel = new(App.Core);
      MediaItemClipsCategory = new();
    }

    public void SetBackgroundBrush(ICatTreeViewItem item, BackgroundBrush backgroundBrush) {
      item.BackgroundBrush = backgroundBrush;
      if (backgroundBrush == BackgroundBrush.Default)
        App.Core.ActiveFilterItems.Remove(item);
      else
        App.Core.ActiveFilterItems.Add(item);

      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterAndCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterOrCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterHiddenCount));
    }

    public async void TreeView_Select(ICatTreeViewItem item, bool and, bool hide, bool recursive, bool loadByTag = false) {
      if (item == null) return;

      if (item is Person p && App.WMain.MainTabs.IsThisContentSet(typeof(FaceRecognitionControl))) {
        var tab = App.WMain.MainTabs.GetTabWithContentTypeOf(typeof(FaceRecognitionControl));
        if (tab?.Content is FaceRecognitionControl frc) frc.ChangePerson(p);
        return;
      }

      // MainTabs Tab Content Type
      switch (item) {
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _:
        case Folder _:
        case FolderKeyword _: {
          App.Ui.MediaItemsViewModel.SetTabContent();
          break;
        }
        case ICatTreeViewGroup _: {
          break;
        }
        case ICatTreeViewCategory _: {
          break;
        }
      }

      switch (item) {
        case FavoriteFolder favoriteFolder: {
          if (favoriteFolder.Folder.IsThisOrParentHidden()) return;
          CatTreeViewUtils.ExpandTo(favoriteFolder.Folder);
          App.WMain.TreeViewCategories.TvCategories.ScrollTo(favoriteFolder.Folder);
          break;
        }
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _: {
          if (App.Core.MediaItems.IsEditModeOn && !loadByTag) {
            if (item is not ICatTreeViewTagItem bti) return;

            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              App.Core.MarkedTags.Add(bti);
            else {
              App.Core.MarkedTags.Remove(bti);
              bti.PicCount = 0;
            }

            App.Core.MediaItems.SetMetadata(item);

            App.Core.MarkUsedKeywordsAndPeople();
            AppInfo.UpdateRating();
          }
          else {
            // get items by tag
            List<MediaItem> items = item switch {
              Rating rating => App.Core.MediaItems.All.Cast<MediaItem>().Where(x => x.Rating == rating.Value).ToList(),
              Person person => person.MediaItems,
              Keyword keyword => keyword.GetMediaItems(recursive).ToList(),
              GeoName geoName => geoName.GetMediaItems(recursive).ToList(),
              _ => new()
            };

            // if CTRL is pressed, add new items to already loaded items
            if (and)
              items = App.Core.MediaItems.ThumbsGrid.LoadedItems.Union(items).ToList();

            // if ALT is pressed, remove new items from already loaded items
            if (hide)
              items = App.Core.MediaItems.ThumbsGrid.LoadedItems.Except(items).ToList();

            await MediaItemsViewModel.LoadAsync(items, null, item.Title);
            App.Core.MarkUsedKeywordsAndPeople();
          }

          break;
        }
        case Folder _:
        case FolderKeyword _: {
          if (item is Folder folder && !folder.IsAccessible) return;

          item.IsSelected = true;

          if (AppInfo.AppMode == AppMode.Viewer)
            WindowCommands.SwitchToBrowser();

          var roots = (item as FolderKeyword)?.Folders ?? new List<Folder> { (Folder)item };
          var folders = Folder.GetFolders(roots, recursive);

          // if CTRL is pressed, add items from new folders to already loaded items
          if (and)
            folders = App.Core.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Union(folders).ToList();

          // if ALT is pressed, remove items from new folders from already loaded items
          if (hide)
            folders = App.Core.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Except(folders).ToList();

          await MediaItemsViewModel.LoadAsync(null, folders, folders[0].Title);
          App.Core.MarkUsedKeywordsAndPeople();
          break;
        }
        case ICatTreeViewCategory _: {
          // if category is going to collapse and sub item is selected, category gets selected
          // and setting IsSelected to false in OnSelectedItemChanged will stop collapsing the category
          // this will only prevent selecting category if selection was made with mouse click
          item.IsSelected = false;
          break;
        }
      }
    }

    public void ActivateFilter(ICatTreeViewItem item, BackgroundBrush mode) {
      SetBackgroundBrush(item, item.BackgroundBrush != BackgroundBrush.Default ? BackgroundBrush.Default : mode);

      // reload with new filter
      MediaItemsViewModel.ReapplyFilter();
    }

    public void ClearFilters() {
      foreach (var item in App.Core.ActiveFilterItems.ToArray())
        SetBackgroundBrush(item, BackgroundBrush.Default);

      // reload with new filter
      MediaItemsViewModel.ReapplyFilter();
    }

    public static CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;
      var srcMi = App.Core.Folders.GetMediaItemByPath(srcFilePath);
      var destMi = App.Core.Folders.GetMediaItemByPath(destFilePath);

      App.Core.RunOnUiThread(() => {
        srcMi?.SetThumbSize();
        srcMi?.SetInfoBox();
        destMi?.SetThumbSize();
        destMi?.SetInfoBox();

        var cd = new FileOperationCollisionDialog(srcFilePath, destFilePath, srcMi, destMi, owner);
        cd.ShowDialog();
        result = cd.Result;
        outFileName = cd.FileName;
      });

      fileName = outFileName;

      return result;
    }

    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      var fops = new PicFileOperationProgressSink();
      using var fo = new FileOperation(fops);
      fo.SetOperationFlags(
        (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
        (silent
          ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
            FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
          : FileOperationFlags.FOF_NOCONFIRMMKDIR));

      foreach (var x in items)
        fo.DeleteItem(x);
      fo.PerformOperations();

      return fops.FileOperationResult;
    }

    public void LogError(Exception ex) {
      LogError(ex, string.Empty);
    }

    public void LogError(Exception ex, string msg) {
      App.Core.RunOnUiThread(() => {
        Log.Add(new LogItem(string.IsNullOrEmpty(msg) ? ex.Message : msg, $"{msg}\n{ex.Message}\n{ex.StackTrace}"));
      });
    }
  }
}
