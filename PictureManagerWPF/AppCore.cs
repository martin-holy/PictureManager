using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.UserControls;
using PictureManager.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager {
  public class AppCore {
    public MediaItemsViewModel MediaItemsViewModel { get; }
    public MediaItemClipsCategory MediaItemClipsCategory { get; }
    public AppInfo AppInfo { get; } = new();

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;

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

    public void TreeView_Select(ICatTreeViewItem item, bool and, bool hide, bool recursive, bool loadByTag = false) {
      if (item == null) return;

      if (item is ICatTreeViewCategory cat && cat is People) {
        var tab = App.WMain.MainTabs.GetTabWithContentTypeOf(typeof(PeopleControl));

        if (tab?.Content is not PeopleControl control) {
          control = new PeopleControl();
          App.WMain.MainTabs.AddTab();
          App.WMain.MainTabs.SetTab(control, control, null);
        }
        else {
          tab.IsSelected = true;
          _ = control.Reload();
        }
      }

      switch (((TabItem)App.WMain.MainTabs.Tabs.SelectedItem).Content) {
        case FaceRecognitionControl frc:
        if (item is Person frcp)
          frc.ChangePerson(frcp);
        break;

        case PeopleControl pc:
        if (item is Person pcp)
          pc.ChangePerson(pcp);
        if (item is Keyword pck)
          pc.ToggleKeyword(pck);
        break;

        case null:
        case MediaItemsThumbsGrid:
        MediaItemsViewModel.SetTabContent();
        switch (item) {
          case FavoriteFolder favoriteFolder:
          if (favoriteFolder.Folder.IsThisOrParentHidden()) return;
          CatTreeViewUtils.ExpandTo(favoriteFolder.Folder);
          App.WMain.TreeViewCategories.TvCategories.ScrollTo(favoriteFolder.Folder);
          break;

          case Rating:
          case Person:
          case Keyword:
          case GeoName:
          if (App.Core.MediaItems.IsEditModeOn && !loadByTag)
            MediaItemsViewModel.SetMetadata(item);
          else
            MediaItemsViewModel.LoadByTag(item, and, hide, recursive);
          break;

          case Folder:
          case FolderKeyword:
          MediaItemsViewModel.LoadByFolder(item, and, hide, recursive);
          break;
        }
        break;
      }

      // if category is going to collapse and sub item is selected, category gets selected
      // and setting IsSelected to false in OnSelectedItemChanged will stop collapsing the category
      // this will only prevent selecting category if selection was made with mouse click
      if (item is ICatTreeViewCategory)
        item.IsSelected = false;
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
  }
}
