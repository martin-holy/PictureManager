using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

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
    AddCategory(Core.KeywordsM.TreeCategory);
    AddCategory(Core.GeoNamesM.TreeCategory);
    AddCategory(Core.ViewersM.TreeCategory);

    Selected = Tabs[0];
  }

  private void AddCategory(TreeCategory cat) {
    cat.IsExpanded = true;
    Add(new ListItem(cat.Icon, cat.Name, cat.TreeView) { IsNameHidden = true });
  }

  // TODO rename, check usage, use it less
  public void MarkUsedKeywordsAndPeople() {
    // can be Person, Keyword, Folder, FolderKeyword, Rating or GeoName

    // clear previous marked tags
    MarkedTags.Clear();

    var mediaItems = Core.MainWindowM.IsFullScreen
      ? Core.MediaItemsM.Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Core.MediaItemsM.Current }
      : Core.MediaItemsViews.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.MediaItemsViews.Current.GetSelectedOrAll().ToArray();

    if (mediaItems.Length == 0) {
      OnPropertyChanged(nameof(MarkedTags));
      return;
    }

    foreach (var mi in mediaItems) {
      // People
      MarkedTagsAddWithIncrease(mi.People
        .EmptyIfNull()
        .Concat(mi.Segments.GetPeople())
        .Distinct()
        .SelectMany(x => x.GetThisAndParents<ITreeItem>()));

      // Keywords
      if (mi.Keywords != null)
        MarkedTagsAddWithIncrease(mi.Keywords.SelectMany(x => x.GetThisAndParents<ITreeItem>()));

      // Folders
      MarkedTagsAddWithIncrease(mi.Folder.GetThisAndParents());

      // FolderKeywords
      if (mi.Folder.FolderKeyword != null)
        MarkedTagsAddWithIncrease(mi.Folder.FolderKeyword.GetThisAndParents());

      // GeoNames
      if (mi.GeoName != null)
        MarkedTagsAddWithIncrease(mi.GeoName.GetThisAndParents());

      // Ratings
      MarkedTagsAddWithIncrease(Core.RatingsTreeCategory.GetRatingByValue(mi.Rating));
    }

    OnPropertyChanged(nameof(MarkedTags));
  }

  private void MarkedTagsAddWithIncrease(IEnumerable<object> tags) {
    foreach (var tag in tags)
      MarkedTagsAddWithIncrease(tag);
  }

  private void MarkedTagsAddWithIncrease(object tag) {
    if (tag == null) return;

    if (MarkedTags.ContainsKey(tag))
      MarkedTags[tag]++;
    else
      MarkedTags.Add(tag, 1);
  }
}