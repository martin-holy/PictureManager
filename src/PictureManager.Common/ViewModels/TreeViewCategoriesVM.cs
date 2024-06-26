﻿using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.TreeCategories;
using System;
using System.Collections.Generic;

namespace PictureManager.Common.ViewModels;

public sealed class TreeViewCategoriesSlotVM;

public sealed class TreeViewCategoriesVM : TabControl {
  public TreeViewSearchVM TreeViewSearch { get; }
  public Dictionary<object, int> MarkedTags { get; } = new();
  public RatingsTreeCategory RatingsTreeCategory { get; } = new();
  public SlidePanelPinButton SlidePanelPinButton { get; } = new();

  public TreeViewCategoriesVM() {
    TreeViewSearch = new(this);
    TabStrip = new() {
      Placement = Dock.Top,
      Slot = new TreeViewCategoriesSlotVM(),
      SlotPlacement = Dock.Right
    };
  }

  public void AddCategories() {
    Tabs.Clear();
    AddCategory(Core.R.FavoriteFolder.Tree);
    AddCategory(Core.R.Folder.Tree);
    AddCategory(RatingsTreeCategory);
    AddCategory(Core.R.Person.Tree);
    AddCategory(Core.R.FolderKeyword.Tree);
    AddCategory(Core.R.Keyword.Tree);
    AddCategory(Core.R.GeoName.Tree);
    AddCategory(Core.R.Viewer.Tree);

    Selected = Tabs[0];
  }

  private void AddCategory(TreeCategory cat) {
    cat.IsExpanded = true;
    Add(new ListItem(cat.Icon, cat.Name, cat.TreeView) { IsNameHidden = true });
  }

  // TODO rename, check usage, use it less
  public void MarkUsedKeywordsAndPeople() {
    MarkedTags.Clear();

    var mediaItems = Core.VM.MainWindow.IsInViewMode
      ? Core.VM.MediaItem.Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Core.VM.MediaItem.Current }
      : Core.VM.MediaItem.Views.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.VM.MediaItem.Views.Current.GetSelectedOrAll().ToArray();

    if (mediaItems.Length == 0) {
      OnPropertyChanged(nameof(MarkedTags));
      return;
    }

    foreach (var mi in mediaItems) {
      Mark(mi.GetPeople(), true);
      Mark(mi.GetKeywords(), true);
      Mark(mi.GetFolders());
      Mark(mi.Folder.GetFolderKeywords());
      Mark(mi.GetGeoNames());
      Mark(RatingsTreeCategory.GetRatingByValue(mi.Rating));
    }

    OnPropertyChanged(nameof(MarkedTags));
  }

  private void Mark(IEnumerable<object> tags, bool withGroups = false) {
    foreach (var tag in tags) {
      Mark(tag);
      if (withGroups)
        Mark(((ITreeItem)tag).GetCategoryGroups());
    }
  }

  private void Mark(object tag) {
    if (tag == null) return;

    if (!MarkedTags.TryAdd(tag, 1))
      MarkedTags[tag]++;
  }
}