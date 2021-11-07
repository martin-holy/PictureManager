﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using PictureManager.UserControls;
using PictureManager.ViewModels;
using PictureManager.ViewModels.Tree;
using PictureManager.Views;


namespace PictureManager {
  public sealed class AppCore {
    public CategoryGroupsBaseVM CategoryGroupsBaseVM { get; }
    public DrivesTreeVM DrivesTreeVM { get; }
    public PeopleBaseVM PeopleBaseVM { get; }
    public KeywordsBaseVM KeywordsBaseVM { get; }
    public ViewersBaseVM ViewersBaseVM { get; }
    public SegmentsBaseVM SegmentsBaseVM { get; }

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

    public MediaItemsViewModel MediaItemsViewModel { get; }
    public AppInfo AppInfo { get; } = new();
    public HashSet<ICatTreeViewTagItem> MarkedTags { get; } = new();
    public HashSet<IFilterItem> ActiveFilterItems { get; } = new();
    public static EventHandler OnToggleKeyword { get; set; }
    public static EventHandler OnSetPerson { get; set; }

    public AppCore() {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;

      AppInfo.ProgressBarValueA = 100;
      AppInfo.ProgressBarValueB = 100;

      MediaItemsViewModel = new(App.Core);
      VideoClipsTreeVM = new(App.Core, App.Core.VideoClipsM);

      CategoryGroupsBaseVM = new();
      DrivesTreeVM = new(this);
      PeopleBaseVM = new(this, App.Core.PeopleM);
      KeywordsBaseVM = new(this, App.Core.KeywordsM);
      ViewersBaseVM = new(this, App.Core.ViewersM);
      SegmentsBaseVM = new(App.Core);

      CategoryGroupsTreeVM = new();
      FavoriteFoldersTreeVM = new(App.Core.FavoriteFoldersM);
      FoldersTreeVM = new(App.Core, this, App.Core.FoldersM);
      RatingsTreeVM = new();
      MediaItemSizesTreeVM = new(this);
      PeopleTreeVM = new(App.Core, this, PeopleBaseVM);
      FolderKeywordsTreeVM = new(App.Core, App.Core.FolderKeywordsM);
      KeywordsTreeVM = new(App.Core, this, KeywordsBaseVM);
      GeoNamesTreeVM = new(App.Core.GeoNamesM);
      ViewersTreeVM = new(ViewersBaseVM);

      TreeViewCategories = new() { FavoriteFoldersTreeVM, FoldersTreeVM, RatingsTreeVM, MediaItemSizesTreeVM, PeopleTreeVM, FolderKeywordsTreeVM, KeywordsTreeVM, GeoNamesTreeVM, ViewersTreeVM };

      FoldersTreeVM.Load();
    }

    public void SetDisplayFilter(IFilterItem item, DisplayFilter displayFilter) {
      item.DisplayFilter = displayFilter;
      if (displayFilter == DisplayFilter.None)
        ActiveFilterItems.Remove(item);
      else
        ActiveFilterItems.Add(item);

      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterAndCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterOrCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterNotCount));
    }

    public static void ToggleKeyword(KeywordTreeVM keyword) {
      var sCount = App.Core.Segments.Selected.Count;
      var pCount = App.Ui.PeopleBaseVM.Selected.Count;
      if (sCount == 0 && pCount == 0) return;

      var msgA = $"Do you want to toggle #{keyword.BaseVM.Model.FullName} on selected";
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
        App.Core.Segments.ToggleKeywordOnSelected(keyword.BaseVM.Model);
      else
        App.Ui.PeopleBaseVM.ToggleKeywordOnSelected(keyword.BaseVM.Model);

      OnToggleKeyword?.Invoke(null, EventArgs.Empty);
    }

    public static void SetPerson(PersonBaseVM person) {
      var sCount = App.Core.Segments.Selected.Count;
      if (sCount == 0) return;

      var msgCount = sCount > 1 ? $"'s ({sCount})" : string.Empty;
      var msg = $"Do you want to set ({person.Model.Name}) to selected segment{msgCount}??";

      if (!MessageDialog.Show("Set Person", msg, true)) return;
      App.Core.Segments.SetSelectedAsPerson(person.Model);
      OnSetPerson?.Invoke(null, EventArgs.Empty);
    }

    public async Task TreeView_Select(ICatTreeViewItem item, bool loadByTag = false) {
      if (item == null) return;

      var and = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var hide = (Keyboard.Modifiers & ModifierKeys.Alt) > 0;
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      if (item is RatingTreeVM or PersonTreeVM or KeywordTreeVM or GeoNameTreeVM) {
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
        case KeywordTreeVM k:
          ToggleKeyword(k);
          break;

        case PersonTreeVM p:
          SetPerson(p.BaseVM);
          break;

        case FavoriteFolderTreeVM ff:
          if (!App.Core.FoldersM.IsFolderVisible(ff.Model.Folder)) break;
          var folderTreeVM = FoldersTreeVM.All[ff.Model.Folder.Id];
          CatTreeView.ExpandTo(folderTreeVM);
          App.WMain.TreeViewCategories.TvCategories.ScrollTo(folderTreeVM);
          break;

        case FolderTreeVM:
        case FolderKeywordTreeVM:
          MediaItemsViewModel.AddThumbsTabIfNotActive();
          await MediaItemsViewModel.LoadByFolder(item, and, hide, recursive);
          break;

        case ViewerTreeVM v:
          App.WMain.MainTabs.ActivateTab<ViewerV>(IconName.Eye)
            ?.Reload(App.Core.ViewersM, v.Model, App.WMain.TreeViewCategories.TvCategories);
          break;

        case ICatTreeViewCategory cat:
          if (cat is PeopleTreeVM)
            _ = App.WMain.MainTabs.ActivateTab<PeopleControl>(IconName.People)?.Reload();

          // if category is going to collapse and sub item is selected, category gets selected
          // and setting IsSelected to false in OnSelectedItemChanged will stop collapsing the category
          // this will only prevent selecting category if selection was made with mouse click
          cat.IsSelected = false;
          break;
      }
    }

    public async Task ActivateFilter(IFilterItem item, DisplayFilter displayFilter) {
      SetDisplayFilter(item, item.DisplayFilter != DisplayFilter.None ? DisplayFilter.None : displayFilter);

      // reload with new filter
      await MediaItemsViewModel.ReapplyFilter();
    }

    public async Task ClearFilters() {
      foreach (var item in ActiveFilterItems.ToArray())
        SetDisplayFilter(item, DisplayFilter.None);

      // reload with new filter
      await MediaItemsViewModel.ReapplyFilter();
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

      if (App.Core.MediaItems.ThumbsGrid == null) return;

      var mediaItems = App.Core.MediaItems.ThumbsGrid.GetSelectedOrAll();
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
        srcMi?.SetInfoBox();
        destMi?.SetThumbSize();
        destMi?.SetInfoBox();

        var cd = new FileOperationCollisionDialog(srcFilePath, destFilePath, srcMi, destMi, owner);
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
