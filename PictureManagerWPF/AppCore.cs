﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.ShellStuff;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.Models;
using PictureManager.ViewModels;
using SimpleDB;

namespace PictureManager {
  public class AppCore: ILogger {
    public Core Model { get; }
    public MediaItemsViewModel MediaItemsViewModel { get; }
    public MediaItemClipsCategory MediaItemClipsCategory { get; }
    public AppInfo AppInfo { get; } = new AppInfo();
    public ObservableCollection<LogItem> Log { get; set; } = new ObservableCollection<LogItem>();

    public AppCore() {
      Model = Core.Instance;
      Model.CachePath = Settings.Default.CachePath;
      Model.ThumbnailSize = Settings.Default.ThumbnailSize;
      Model.Logger = this;

      AppInfo.ProgressBarValueA = 100;
      AppInfo.ProgressBarValueB = 100;

      MediaItemsViewModel = new MediaItemsViewModel(this);
      MediaItemClipsCategory = new MediaItemClipsCategory();
    }

    public void SetBackgroundBrush(ICatTreeViewItem item, BackgroundBrush backgroundBrush) {
      item.BackgroundBrush = backgroundBrush;
      if (backgroundBrush == BackgroundBrush.Default)
        Model.ActiveFilterItems.Remove(item);
      else
        Model.ActiveFilterItems.Add(item);

      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterAndCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterOrCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterHiddenCount));
    }

    public async void TreeView_Select(ICatTreeViewItem item, bool and, bool hide, bool recursive, bool loadByTag = false) {
      if (item == null) return;

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
          if (Model.MediaItems.IsEditModeOn && !loadByTag) {
            if (!(item is ICatTreeViewTagItem bti)) return;

            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              Model.MarkedTags.Add(bti);
            else {
              Model.MarkedTags.Remove(bti);
              bti.PicCount = 0;
            }

            Model.MediaItems.SetMetadata(item);

            Model.MarkUsedKeywordsAndPeople();
            AppInfo.UpdateRating();
          }
          else {
            // get items by tag
            var items = new List<MediaItem>();

            switch (item) {
              case Rating rating: items = Model.MediaItems.All.Cast<MediaItem>().Where(x => x.Rating == rating.Value).ToList(); break;
              case Keyword keyword: items = keyword.GetMediaItems(recursive).ToList(); break;
              case Person person: items = person.MediaItems; break;
              case GeoName geoName: items = geoName.GetMediaItems(recursive).ToList(); break;
            }

            // if CTRL is pressed, add new items to already loaded items
            if (and)
              items = Model.MediaItems.ThumbsGrid.LoadedItems.Union(items).ToList();

            // if ALT is pressed, remove new items from already loaded items
            if (hide)
              items = Model.MediaItems.ThumbsGrid.LoadedItems.Except(items).ToList();

            await MediaItemsViewModel.LoadAsync(items, null, item.Title);
            Model.MarkUsedKeywordsAndPeople();
          }

          break;
        }
        case Folder _:
        case FolderKeyword _: {
          if (item is Folder folder && !folder.IsAccessible) return;

          item.IsSelected = true;

          if (AppInfo.AppMode == AppMode.Viewer) {
            App.WMain.CommandsController.WindowCommands.SwitchToBrowser();
          }

          var roots = (item as FolderKeyword)?.Folders ?? new List<Folder> {(Folder) item};
          var folders = Folder.GetFolders(roots, recursive);

          // if CTRL is pressed, add items from new folders to already loaded items
          if (and)
            folders = Model.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Union(folders).ToList();

          // if ALT is pressed, remove items from new folders from already loaded items
          if (hide)
            folders = Model.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Except(folders).ToList();

          await MediaItemsViewModel.LoadAsync(null, folders, folders[0].Title);
          Model.MarkUsedKeywordsAndPeople();
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
      foreach (var item in Model.ActiveFilterItems.ToArray())
        SetBackgroundBrush(item, BackgroundBrush.Default);

      // reload with new filter
      MediaItemsViewModel.ReapplyFilter();
    }

    public static CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;
      var srcMi = App.Core.Model.Folders.GetMediaItemByPath(srcFilePath);
      var destMi = App.Core.Model.Folders.GetMediaItemByPath(destFilePath);

      Application.Current.Dispatcher?.Invoke(delegate {
        srcMi?.SetThumbSize();
        srcMi?.SetInfoBox();
        destMi?.SetThumbSize();
        destMi?.SetInfoBox();

        var focd = new FileOperationCollisionDialog(srcFilePath, destFilePath, srcMi, destMi, owner);
        focd.ShowDialog();
        result = focd.Result;
        outFileName = focd.FileName;
      });

      fileName = outFileName;

      return result;
    }

    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      var fops = new PicFileOperationProgressSink();
      using (var fo = new FileOperation(fops)) {
        fo.SetOperationFlags(
          (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
          (silent
            ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
              FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
            : FileOperationFlags.FOF_NOCONFIRMMKDIR));

        items.ForEach(x => fo.DeleteItem(x));
        fo.PerformOperations();
      }

      return fops.FileOperationResult;
    }

    public void LogError(Exception ex) {
      LogError(ex, string.Empty);
    }

    public void LogError(Exception ex, string msg) {
      Application.Current.Invoke(delegate {
        Log.Add(new LogItem(string.IsNullOrEmpty(msg) ? ex.Message : msg, $"{msg}\n{ex.Message}\n{ex.StackTrace}"));
      });
    }
  }
}
