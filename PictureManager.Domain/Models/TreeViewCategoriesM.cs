using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;

namespace PictureManager.Domain.Models;

public sealed class TreeViewCategoriesM : TabControl {
  public TreeViewSearchM TreeViewSearchM { get; } = new();
  public Dictionary<object, int> MarkedTags { get; } = new();

  public void AddCategories() {
    Tabs.Clear();
    AddCategory(Core.Db.FavoriteFolders.Model);
    AddCategory(Core.FoldersM.TreeCategory);
    AddCategory(Core.RatingsTreeCategory);
    AddCategory(Core.PeopleM.TreeCategory);
    AddCategory(Core.Db.FolderKeywords.Model);
    AddCategory(Core.Db.Keywords.Tree);
    AddCategory(Core.Db.GeoNames.Tree);
    AddCategory(Core.ViewersM.TreeCategory);

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
      ? Core.VM.MediaItems.Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Core.VM.MediaItems.Current }
      : Core.MediaItemsViews.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.MediaItemsViews.Current.GetSelectedOrAll().ToArray();

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
      Mark(Core.RatingsTreeCategory.GetRatingByValue(mi.Rating));
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

    if (MarkedTags.ContainsKey(tag))
      MarkedTags[tag]++;
    else
      MarkedTags.Add(tag, 1);
  }
}