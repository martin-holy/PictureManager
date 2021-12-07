using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils.Extensions;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.UserControls;
using PictureManager.Utils;
using PictureManager.ViewModels;
using PictureManager.ViewModels.Tree;
using PictureManager.Views;

namespace PictureManager {
  public sealed class AppCore {
    public DrivesTreeVM DrivesTreeVM { get; }
    public SegmentsBaseVM SegmentsBaseVM { get; }
    public MediaItemsBaseVM MediaItemsBaseVM { get; }
    public ThumbnailsGridsVM ThumbnailsGridsVM { get; }

    #region TreeView Roots and Categories
    public ObservableCollection<ICatTreeViewCategory> TreeViewCategories { get; }
    public CategoryGroupsTreeVM CategoryGroupsTreeVM { get; }
    public FavoriteFoldersTreeVM FavoriteFoldersTreeVM { get; }
    public FoldersTreeVM FoldersTreeVM { get; }
    public RatingsTreeVM RatingsTreeVM { get; }
    public MediaItemSizesTreeVM MediaItemSizesTreeVM { get; }
    public PeopleTreeVM PeopleTreeVM { get; }
    public FolderKeywordsTreeVM FolderKeywordsTreeVM { get; }
    public KeywordsTreeVM KeywordsTreeVM { get; }
    public GeoNamesTreeVM GeoNamesTreeVM { get; }
    public ViewersTreeVM ViewersTreeVM { get; }
    public VideoClipsTreeVM VideoClipsTreeVM { get; }
    #endregion

    public MainTabsVM MainTabsVM { get; }

    public AppInfo AppInfo { get; } = new();
    public HashSet<ICatTreeViewTagItem> MarkedTags { get; } = new();
    public static EventHandler OnToggleKeyword { get; set; }
    public static EventHandler OnSetPerson { get; set; }

    #region Commands
    public RelayCommand<PersonM> SetCurrentPersonCommand { get; }
    #endregion

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;

      MainTabsVM = new();

      AppInfo.ProgressBarValueA = 100;
      AppInfo.ProgressBarValueB = 100;

      VideoClipsTreeVM = new(App.Core, App.Core.VideoClipsM);

      DrivesTreeVM = new(this);
      SegmentsBaseVM = new(App.Core, this, App.Core.SegmentsM);
      MediaItemsBaseVM = new(App.Core, this, App.Core.MediaItemsM);
      ThumbnailsGridsVM = new(App.Core, this, App.Core.ThumbnailsGridsM);

      CategoryGroupsTreeVM = new();
      FavoriteFoldersTreeVM = new(App.Core.FavoriteFoldersM);
      FoldersTreeVM = new(App.Core, this, App.Core.FoldersM);
      RatingsTreeVM = new();
      MediaItemSizesTreeVM = new(ThumbnailsGridsVM);
      PeopleTreeVM = new(App.Core, this, App.Core.PeopleM);
      FolderKeywordsTreeVM = new(App.Core, App.Core.FolderKeywordsM);
      KeywordsTreeVM = new(App.Core, this, App.Core.KeywordsM);
      GeoNamesTreeVM = new(App.Core.GeoNamesM);
      ViewersTreeVM = new(App.Core.ViewersM);

      TreeViewCategories = new() { FavoriteFoldersTreeVM, FoldersTreeVM, RatingsTreeVM, MediaItemSizesTreeVM, PeopleTreeVM, FolderKeywordsTreeVM, KeywordsTreeVM, GeoNamesTreeVM, ViewersTreeVM };

      FoldersTreeVM.Load();

      #region Commands
      SetCurrentPersonCommand = new(
        person => App.Core.PeopleM.Current = App.Core.PeopleM.All[person.Id],
        person => person != null);
      #endregion
    }

    private static void ToggleKeyword(KeywordTreeVM keyword) {
      var sCount = App.Core.SegmentsM.Selected.Count;
      var pCount = App.Core.PeopleM.Selected.Count;
      if (sCount == 0 && pCount == 0) return;

      var msgA = $"Do you want to toggle #{keyword.Model.FullName} on selected";
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
        App.Core.SegmentsM.ToggleKeywordOnSelected(keyword.Model);
      else
        App.Core.PeopleM.ToggleKeywordOnSelected(keyword.Model);

      OnToggleKeyword?.Invoke(null, EventArgs.Empty);
    }

    private static void SetPerson(PersonM person) {
      var sCount = App.Core.SegmentsM.Selected.Count;
      if (sCount == 0) return;

      var msgCount = sCount > 1 ? $"'s ({sCount})" : string.Empty;
      var msg = $"Do you want to set ({person.Name}) to selected segment{msgCount}??";

      if (!MessageDialog.Show("Set Person", msg, true)) return;
      App.Core.SegmentsM.SetSelectedAsPerson(person);
      OnSetPerson?.Invoke(null, EventArgs.Empty);
    }

    public async Task TreeView_Select(ICatTreeViewItem item) {
      if (item == null) return;

      if (item is RatingTreeVM or PersonTreeVM or KeywordTreeVM or GeoNameTreeVM) {
        if (App.Core.MediaItemsM.IsEditModeOn && item is ICatTreeViewTagItem tagItem) {
          if (!MarkedTags.Toggle(tagItem))
            tagItem.PicCount = 0;

          MediaItemsBaseVM.SetMetadata(tagItem);

          MarkUsedKeywordsAndPeople();
          App.WMain.StatusPanel.UpdateRating();

          return;
        }
      }

      switch (item) {
        case KeywordTreeVM k:
          ToggleKeyword(k);
          break;

        case PersonTreeVM p:
          SetPerson(p.Model);
          break;

        case FavoriteFolderTreeVM ff:
          if (!App.Core.FoldersM.IsFolderVisible(ff.Model.Folder)) break;
          var folderTreeVM = FoldersTreeVM.All[ff.Model.Folder.Id];
          CatTreeView.ExpandTo(folderTreeVM);
          App.WMain.TreeViewCategories.TvCategories.ScrollTo(folderTreeVM);
          break;

        case FolderTreeVM:
        case FolderKeywordTreeVM:
          var (and, hide, recursive) = InputUtils.GetControlAltShiftModifiers();
          await ThumbnailsGridsVM.LoadByFolder(item, and, hide, recursive);
          break;

        case ViewerTreeVM v:
          MainTabsVM.ActivateTab<ViewerV>()?.Reload(App.Core.ViewersM, v.Model, App.WMain.TreeViewCategories.TvCategories);
          break;

        case ICatTreeViewCategory cat:
          if (cat is PeopleTreeVM)
            _ = MainTabsVM.ActivateTab<PeopleControl>()?.Reload();

          // if category is going to collapse and sub item is selected, category gets selected
          // and setting IsSelected to false in OnSelectedItemChanged will stop collapsing the category
          // this will only prevent selecting category if selection was made with mouse click
          cat.IsSelected = false;
          break;
      }
    }

    public void MarkUsedKeywordsAndPeople() {
      //can be Person, Keyword, FolderKeyword, Rating or GeoName

      void MarkedTagsAddWithIncrease(ICatTreeViewTagItem item) {
        if (item == null) return;
        item.PicCount++;
        if (!MarkedTags.Contains(item))
          MarkedTags.Add(item);
      }

      // clear previous marked tags
      foreach (var item in MarkedTags)
        item.PicCount = 0;
      MarkedTags.Clear();

      if (App.Core.ThumbnailsGridsM.Current == null) return;

      var mediaItems = App.Core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      foreach (var mi in mediaItems) {

        // People
        if (mi.People != null)
          foreach (var person in mi.People) {
            var vm = PeopleTreeVM.All[person.Id];
            MarkedTagsAddWithIncrease(vm);
            MarkedTagsAddWithIncrease(vm.Parent as CategoryGroupTreeVM);
          }

        // Keywords
        if (mi.Keywords != null) {
          foreach (var keyword in mi.Keywords) {
            var k = keyword;
            while (k != null) {
              var vm = KeywordsTreeVM.All[k.Id];
              MarkedTagsAddWithIncrease(vm);
              MarkedTagsAddWithIncrease(vm.Parent as CategoryGroupTreeVM);
              k = k.Parent as KeywordM;
            }
          }
        }

        // Folders
        var f = mi.Folder;
        while (f != null) {
          var vm = f.Parent is FoldersM ? DrivesTreeVM.All[f.Id] : FoldersTreeVM.All[f.Id];
          MarkedTagsAddWithIncrease(vm);
          f = f.Parent as FolderM;
        }

        // FolderKeywords
        var fk = mi.Folder.FolderKeyword;
        while (fk != null) {
          var vm = FolderKeywordsTreeVM.All[fk.Id];
          MarkedTagsAddWithIncrease(vm);
          fk = fk.Parent as FolderKeywordM;
        }

        // GeoNames
        var gn = mi.GeoName;
        while (gn != null) {
          var vm = GeoNamesTreeVM.All[gn.Id];
          MarkedTagsAddWithIncrease(vm);
          gn = gn.Parent as GeoNameM;
        }

        // Ratings
        MarkedTagsAddWithIncrease(App.Ui.RatingsTreeVM.GetRatingByValue(mi.Rating));
      }
    }

    public static CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;
      var srcMi = App.Core.FoldersM.GetMediaItemByPath(srcFilePath);
      var destMi = App.Core.FoldersM.GetMediaItemByPath(destFilePath);

      App.Core.RunOnUiThread(() => {
        srcMi?.SetThumbSize();
        destMi?.SetThumbSize();
        var srcMiVM = App.Ui.MediaItemsBaseVM.ToViewModel(srcMi);
        var destMiVM = App.Ui.MediaItemsBaseVM.ToViewModel(destMi);
        srcMiVM?.SetInfoBox();
        destMiVM?.SetInfoBox();

        var cd = new FileOperationCollisionDialog(srcFilePath, destFilePath, srcMiVM, destMiVM, owner);
        cd.ShowDialog();
        result = cd.Result;
        outFileName = cd.FileName;
      }).GetAwaiter().GetResult();

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
