using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewCategoriesM : ObservableObject {
    private readonly Core _core;
    private ITreeItem _scrollToItem;
    private bool _isOpen = true;
    private bool _isPinned = true;
    
    public ObservableCollection<TreeCategory> Items { get; }
    public TreeViewSearchM TreeViewSearchM { get; }
    public Dictionary<object, int> MarkedTags { get; } = new();
    public bool IsPinnedInViewer { get; set; }
    public bool IsPinnedInBrowser { get; set; } = true;
    
    public ITreeItem ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public bool IsPinned {
      get => _isPinned;
      set {
        _isPinned = value;
        IsOpen = value;
        OnPropertyChanged();
      }
    }

    public TreeViewCategoriesM(Core core) {
      _core = core;

      TreeViewSearchM = new(_core);

      Items = new() {
        _core.FavoriteFoldersM,
        _core.FoldersM,
        _core.RatingsTreeM,
        _core.MediaItemSizesTreeM,
        _core.PeopleM,
        _core.FolderKeywordsM,
        _core.KeywordsM,
        _core.GeoNamesM,
        _core.ViewersM };
    }

    public void SetIsPinned(bool inViewer) =>
      IsPinned = inViewer
        ? IsPinnedInViewer
        : IsPinnedInBrowser;

    public void ShowSearch() {
      TreeViewSearchM.SearchText = string.Empty;
      TreeViewSearchM.IsVisible = true;
    }

    public void TagItemDeleteNotUsed(ITreeItem root) {
      if (Core.DialogHostShow(new MessageDialog(
        "Delete Confirmation",
        $"Do you really want to delete not used items in '{root.Name}'?",
        Res.IconQuestion,
        true)) != 0) return;

      switch (MH.Utils.Tree.GetTopParent(root)) {
        case PeopleM:
          _core.PeopleM.DeleteNotUsed(
            root.Items.OfType<PersonM>(),
            _core.MediaItemsM.All);
          break;

        case KeywordsM:
          _core.KeywordsM.DeleteNotUsed(
            root.Items.OfType<KeywordM>(),
            _core.MediaItemsM.All,
            _core.PeopleM.All);
          break;
      }
    }

    // TODO rename, check usage, use it less
    public void MarkUsedKeywordsAndPeople() {
      // can be Person, Keyword, Folder, FolderKeyword, Rating or GeoName

      void MarkedTagsAddWithIncrease(object tag) {
        if (tag == null) return;

        if (MarkedTags.ContainsKey(tag))
          MarkedTags[tag]++;
        else
          MarkedTags.Add(tag, 1);
      }

      // clear previous marked tags
      MarkedTags.Clear();

      if (_core.ThumbnailsGridsM.Current == null) {
        OnPropertyChanged(nameof(MarkedTags));
        return;
      }

      var mediaItems = _core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      foreach (var mi in mediaItems) {

        // People
        if (mi.People != null)
          foreach (var person in mi.People) {
            MarkedTagsAddWithIncrease(person);
            MarkedTagsAddWithIncrease(person.Parent as CategoryGroupM);
          }

        // Keywords
        if (mi.Keywords != null) {
          foreach (var keyword in mi.Keywords) {
            var k = keyword;
            while (k != null) {
              MarkedTagsAddWithIncrease(k);
              MarkedTagsAddWithIncrease(k.Parent as CategoryGroupM);
              k = k.Parent as KeywordM;
            }
          }
        }

        // Folders
        var f = mi.Folder;
        while (f != null) {
          MarkedTagsAddWithIncrease(f);
          f = f.Parent as FolderM;
        }

        // FolderKeywords
        var fk = mi.Folder.FolderKeyword;
        while (fk != null) {
          MarkedTagsAddWithIncrease(fk);
          fk = fk.Parent as FolderKeywordM;
        }

        // GeoNames
        var gn = mi.GeoName;
        while (gn != null) {
          MarkedTagsAddWithIncrease(gn);
          gn = gn.Parent as GeoNameM;
        }

        // Ratings
        MarkedTagsAddWithIncrease(_core.RatingsTreeM.GetRatingByValue(mi.Rating));
      }

      OnPropertyChanged(nameof(MarkedTags));
    }
  }
}
