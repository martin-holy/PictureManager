using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Rating;
using System.Collections.Generic;

namespace PictureManager.Common.Features.Common;

public sealed class TreeViewCategoriesSlotVM;

public sealed class TreeViewCategoriesVM : TabControl {
  public TreeViewSearchVM TreeViewSearch { get; }
  public Dictionary<object, int> MarkedTags { get; } = new();
  public RatingTreeCategory RatingsTreeCategory { get; } = new();
  public SlidePanelPinButton SlidePanelPinButton { get; } = new();

  public TreeViewCategoriesVM() :
    base(new(Dock.Top, Dock.Right, new TreeViewCategoriesSlotVM()) { IconTextVisibility = IconTextVisibility.Icon }) {
    TreeViewSearch = new(this);
  }

  public void AddCategories() {
    Tabs.Clear();
    _addCategory(Core.R.FavoriteFolder.Tree);
    _addCategory(Core.R.Folder.Tree.Category);
    _addCategory(RatingsTreeCategory);
    _addCategory(Core.R.Person.Tree);
    _addCategory(Core.R.FolderKeyword.Tree.Category);
    _addCategory(Core.R.Keyword.Tree);
    _addCategory(Core.R.GeoName.Tree);
    _addCategory(Core.R.Viewer.Tree);

    Selected = Tabs[0];
  }

  private void _addCategory(TreeCategory cat) {
    cat.IsExpanded = true;
    Add(new ListItem(cat.Icon, cat.Name, cat.TreeView) { IsNameHidden = true });
  }

  // TODO rename, check usage, use it less
  public void MarkUsedKeywordsAndPeople() {
    MarkedTags.Clear();

    var mediaItems = Core.VM.MainWindow.IsInViewMode
      ? Core.VM.MediaItem.Current == null
        ? []
        : [Core.VM.MediaItem.Current]
      : Core.VM.MediaItem.Views.Current == null
        ? []
        : Core.VM.MediaItem.Views.Current.GetSelectedOrAll().ToArray();

    if (mediaItems.Length == 0) {
      OnPropertyChanged(nameof(MarkedTags));
      return;
    }

    foreach (var mi in mediaItems) {
      _mark(mi.GetPeople(), true);
      _mark(mi.GetKeywords(), true);
      _mark(mi.GetFolders());
      _mark(mi.Folder.GetFolderKeywords());
      _mark(mi.GetGeoNames());
      _mark(RatingsTreeCategory.GetRatingByValue(mi.Rating));
    }

    OnPropertyChanged(nameof(MarkedTags));
  }

  private void _mark(IEnumerable<object> tags, bool withGroups = false) {
    foreach (var tag in tags) {
      _mark(tag);
      if (withGroups)
        _mark(((ITreeItem)tag).GetCategoryGroups());
    }
  }

  private void _mark(object tag) {
    if (!MarkedTags.TryAdd(tag, 1))
      MarkedTags[tag]++;
  }
}