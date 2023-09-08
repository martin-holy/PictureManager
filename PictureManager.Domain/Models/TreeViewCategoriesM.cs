using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewCategoriesM : TabControl {
    private readonly Core _core;

    public TreeViewSearchM TreeViewSearchM { get; }
    public Dictionary<object, int> MarkedTags { get; } = new();

    public TreeViewCategoriesM(Core core) {
      _core = core;
      TreeViewSearchM = new(_core);

      Add(_core.FavoriteFoldersM);
      Add(_core.FoldersM);
      Add(_core.RatingsTreeM);
      Add(_core.PeopleM);
      Add(_core.FolderKeywordsM);
      Add(_core.KeywordsM);
      Add(_core.GeoNamesM);
      Add(_core.ViewersM);

      Selected = Tabs[0];
    }

    private void Add(TreeCategoryBase cat) {
      cat.IsExpanded = true;
      Add(new ListItem(cat.Icon, cat.Name, cat.TreeView) { IsNameHidden = true });
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

      var mediaItems = _core.MainWindowM.IsFullScreen
        ? _core.MediaItemsM.Current == null
          ? Array.Empty<MediaItemM>()
          : new[] { _core.MediaItemsM.Current }
        : _core.MediaItemsViews.Current == null
          ? Array.Empty<MediaItemM>()
          : _core.MediaItemsViews.Current.GetSelectedOrAll().ToArray();

      if (mediaItems.Length == 0) {
        OnPropertyChanged(nameof(MarkedTags));
        return;
      }

      foreach (var mi in mediaItems) {
        // People
        if (mi.People != null || mi.Segments != null) {
          var people = (
              mi.People == null
                ? Array.Empty<PersonM>()
                : mi.People.ToArray())
            .Concat(
              mi.Segments == null
                ? Array.Empty<PersonM>()
                : mi.Segments
                  .Where(x => x.Person?.Id > 0)
                  .Select(x => x.Person)
                  .ToArray())
            .Distinct();

          foreach (var person in people) {
            MarkedTagsAddWithIncrease(person);
            MarkedTagsAddWithIncrease(person.Parent as CategoryGroupM);
          }
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
