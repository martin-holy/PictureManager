using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.ShellStuff;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.Models;
using PictureManager.ViewModels;
using SimpleDB;

namespace PictureManager {
  public class AppCore: ILogger {
    public Core Model { get; }
    public MediaItemsViewModel MediaItemsViewModel { get; }
    public AppInfo AppInfo { get; } = new AppInfo();
    public ObservableCollection<LogItem> Log { get; set; } = new ObservableCollection<LogItem>();

    public AppCore() {
      Model = Core.Instance;
      Model.CachePath = Settings.Default.CachePath;
      Model.ThumbnailSize = Settings.Default.ThumbnailSize;
      Model.Logger = this;

      AppInfo.ProgressBarValueA = 100;
      AppInfo.ProgressBarValueB = 100;

      MediaItemsViewModel = new MediaItemsViewModel();

      Model.MediaItems.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName.Equals(nameof(Model.MediaItems.Current))) {
          AppInfo.CurrentMediaItem = Model.MediaItems.Current;
        }
      };
    }

    public void SetBackgroundBrush(BaseTreeViewItem item, BackgroundBrush backgroundBrush) {
      item.BackgroundBrush = backgroundBrush;
      if (backgroundBrush == BackgroundBrush.Default)
        Model.ActiveFilterItems.Remove(item);
      else
        Model.ActiveFilterItems.Add(item);

      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterAndCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterOrCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterHiddenCount));
    }

    public async void TreeView_Select(BaseTreeViewItem item, bool and, bool hide, bool recursive) {
      if (item == null) return;

      switch (item) {
        case FavoriteFolder favoriteFolder: {
          if (favoriteFolder.Folder.IsThisOrParentHidden()) return;
          BaseTreeViewItem.ExpandTo(favoriteFolder.Folder);

          // scroll to folder
          var visibleTreeIndex = 0;
          Model.Folders.GetVisibleTreeIndexFor(Model.Folders.Items, favoriteFolder.Folder, ref visibleTreeIndex);
          var offset = (Model.FavoriteFolders.Items.Count + visibleTreeIndex) * 25;
          var border = VisualTreeHelper.GetChild(App.WMain.TvFolders, 0);
          var scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
          scrollViewer?.ScrollToVerticalOffset(offset);
          break;
        }
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _: {
          if (Model.MediaItems.IsEditModeOn) {
            if (!(item is BaseTreeViewTagItem bti)) return;

            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              Model.MarkedTags.Add(bti);
            else {
              Model.MarkedTags.Remove(bti);
              bti.PicCount = 0;
            }

            Model.MediaItems.SetMetadata(item);

            Model.MarkUsedKeywordsAndPeople();
          }
          else {
            // get items by tag
            List<MediaItem> items = null;

            switch ((BaseTreeViewTagItem)item) {
              case Rating rating: items = Model.MediaItems.All.Where(x => x.Rating == rating.Value).ToList(); break;
              case Keyword keyword: items = keyword.GetMediaItems(recursive).ToList(); break;
              case Person person: items = person.MediaItems; break;
              case GeoName geoName: items = geoName.GetMediaItems(recursive).ToList(); break;
            }
            await MediaItemsViewModel.LoadAsync(items, null);
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
          
          await MediaItemsViewModel.LoadAsync(null, folders);
          Model.MarkUsedKeywordsAndPeople();
          break;
        }
      }
    }

    public void ActivateFilter(BaseTreeViewItem item, BackgroundBrush mode) {
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

      Application.Current.Dispatcher?.Invoke(delegate {
        var focd = new FileOperationCollisionDialog(srcFilePath, destFilePath, owner);
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
