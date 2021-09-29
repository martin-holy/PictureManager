using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.UserControls;
using PictureManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager {
  public sealed class AppCore {
    public MediaItemsViewModel MediaItemsViewModel { get; }
    public MediaItemClipsCategory MediaItemClipsCategory { get; }
    public AppInfo AppInfo { get; } = new();
    public static EventHandler OnToggleKeyword { get; set; }
    public static EventHandler OnSetPerson { get; set; }

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

    public static void ToggleKeyword(Keyword keyword) {
      var sCount = App.Core.Segments.Selected.Count;
      var pCount = App.Core.People.Selected.Count;
      if (sCount == 0 && pCount == 0) return;

      var msgA = $"Do you want to toggle #{keyword.FullPath} on selected";
      var msgS = sCount > 1 ? "Segments" : "Segment";
      var msgP = pCount > 1 ? "People" : "Person";
      var msgSCount = sCount > 1 ? $" ({sCount})" : string.Empty;
      var msgPCount = pCount > 1 ? $" ({pCount})" : string.Empty;

      bool? result = null;
      if (sCount > 0 && pCount > 0)
        result = MessageDialog.Show("Toggle Keyword", $"{msgA} {msgS} or {msgP}?", true, msgS + msgSCount, msgP + msgPCount);
      else if (sCount > 0)
        result = MessageDialog.Show("Toggle Keyword", $"{msgA} {msgS}{msgSCount}?", true) ? true : null;
      else if (pCount > 0)
        result = MessageDialog.Show("Toggle Keyword", $"{msgA} {msgP}{msgPCount}?", true) ? false : null;

      if (result == null) return;
      if (result == true)
        App.Core.Segments.ToggleKeywordOnSelected(keyword);
      else
        App.Core.People.ToggleKeywordOnSelected(keyword);

      OnToggleKeyword?.Invoke(null, EventArgs.Empty);
    }

    public static void SetPerson(Person person) {
      var sCount = App.Core.Segments.Selected.Count;
      if (sCount == 0) return;

      var msgCount = sCount > 1 ? $"'s ({sCount})" : string.Empty;
      var msg = $"Do you want to set ({person.Title}) to selected segment{msgCount}??";

      if (!MessageDialog.Show("Set Person", msg, true)) return;
      App.Core.Segments.SetSelectedAsPerson(person);
      OnSetPerson?.Invoke(null, null);
    }

    public async Task TreeView_Select(ICatTreeViewItem item, bool and, bool hide, bool recursive, bool loadByTag = false) {
      if (item == null) return;

      if (item is Rating or Person or Keyword or GeoName) {
        if (loadByTag) {
          MediaItemsViewModel.AddThumbsTabIfNotActive();
          await MediaItemsViewModel.LoadByTag(item, and, hide, recursive);
          return;
        }
        else if (App.Core.MediaItems.IsEditModeOn) {
          MediaItemsViewModel.SetMetadata(item);
          return;
        }
      }

      switch (item) {
        case Keyword k:
        ToggleKeyword(k);
        break;

        case Person p:
        SetPerson(p);
        break;

        case FavoriteFolder ff:
        if (ff.Folder.IsThisOrParentHidden()) break;
        CatTreeViewUtils.ExpandTo(ff.Folder);
        App.WMain.TreeViewCategories.TvCategories.ScrollTo(ff.Folder);
        break;

        case Folder:
        case FolderKeyword:
        MediaItemsViewModel.AddThumbsTabIfNotActive();
        await MediaItemsViewModel.LoadByFolder(item, and, hide, recursive);
        break;

        case Viewer v:
        App.WMain.MainTabs.ActivateTab<ViewerView>(IconName.Eye)?.Reload(v);
        break;

        case ICatTreeViewCategory cat:
        if (cat is People)
          _ = App.WMain.MainTabs.ActivateTab<PeopleControl>(IconName.People)?.Reload();

        // if category is going to collapse and sub item is selected, category gets selected
        // and setting IsSelected to false in OnSelectedItemChanged will stop collapsing the category
        // this will only prevent selecting category if selection was made with mouse click
        cat.IsSelected = false;
        break;
      }
    }

    public async Task ActivateFilter(ICatTreeViewItem item, BackgroundBrush mode) {
      SetBackgroundBrush(item, item.BackgroundBrush != BackgroundBrush.Default ? BackgroundBrush.Default : mode);

      // reload with new filter
      await MediaItemsViewModel.ReapplyFilter();
    }

    public async Task ClearFilters() {
      foreach (var item in App.Core.ActiveFilterItems.ToArray())
        SetBackgroundBrush(item, BackgroundBrush.Default);

      // reload with new filter
      await MediaItemsViewModel.ReapplyFilter();
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
