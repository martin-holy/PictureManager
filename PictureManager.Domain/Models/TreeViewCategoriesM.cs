using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.Dialogs;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewCategoriesM : ObservableObject {
    private readonly Core _core;
    private ITreeItem _scrollToItem;
    
    public ObservableCollection<TreeCategory> Items { get; }
    public TreeViewSearchM TreeViewSearchM { get; }
    public Dictionary<object, int> MarkedTags { get; } = new();
    public ITreeItem ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public RelayCommand<object> ShowSearchCommand { get; }
    public RelayCommand<ITreeItem> ScrollToCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public TreeViewCategoriesM(Core core) {
      _core = core;

      TreeViewSearchM = new(_core);

      ShowSearchCommand = new(ShowSearch);
      ScrollToCommand = new(ScrollTo);
      SelectCommand = new(Select);

      Items = new() {
        _core.FavoriteFoldersM,
        _core.FoldersM,
        _core.RatingsTreeM,
        _core.PeopleM,
        _core.FolderKeywordsM,
        _core.KeywordsM,
        _core.GeoNamesM,
        _core.ViewersM };

      AttachEvents();
    }

    private void AttachEvents() {
      _core.FoldersM.AfterItemRenameEventHandler += (_, e) => {
        // reload if the folder was selected before
        if (e.Data is FolderM { IsSelected: true } folder)
          Select(new MouseButtonEventArgs() { DataContext = folder });
      };
      _core.FoldersM.AfterItemCreateEventHandler += (_, e) =>
        ScrollToItem = e.Data;
      _core.PeopleM.AfterItemCreateEventHandler += (_, e) =>
        ScrollToItem = e.Data;
      _core.KeywordsM.AfterItemCreateEventHandler += (_, e) =>
        ScrollToItem = e.Data;
    }

    private void ScrollTo(ITreeItem item) =>
      ScrollToItem = item;

    private void ShowSearch() {
      TreeViewSearchM.SearchText = string.Empty;
      TreeViewSearchM.IsVisible = true;
    }

    public void Select(MouseButtonEventArgs e) {
      // SHIFT key => recursive
      // (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide

      if (e.DataContext is not ITreeItem item) return;

      switch (item) {
        case RatingTreeM r:
          if (_core.MediaItemsM.IsEditModeOn)
            _core.MediaItemsM.SetMetadata(item);
          else
            _core.ThumbnailsGridsM.Current?.Filter.Set(r.Rating, DisplayFilter.Or);
          break;

        case GeoNameM:
          if (_core.MediaItemsM.IsEditModeOn)
            _core.MediaItemsM.SetMetadata(item);
          break;

        case KeywordM k:
          ToggleDialogM.ToggleKeyword(_core, k);
          break;

        case PersonM p:
          ToggleDialogM.TogglePerson(_core, p);
          break;

        case FavoriteFolderM ff:
          if (!_core.FoldersM.IsFolderVisible(ff.Folder)) break;
          ff.Folder.ExpandTo();
          ScrollToItem = ff.Folder;
          break;

        case FolderM:
        case FolderKeywordM:
          if (_core.MediaViewerM.IsVisible)
            _core.MainWindowM.IsFullScreen = false;

          _ = _core.ThumbnailsGridsM.LoadByFolder(item, e.IsCtrlOn, e.IsAltOn, e.IsShiftOn);
          break;

        case ViewerM v:
          _core.MainTabsM.Activate(_core.ViewerDetailM, "Viewer");
          _core.ViewersM.Selected = v;
          break;

        case ITreeCategory cat:
          if (cat is PeopleM)
            _core.PeopleM.OpenPeopleView();
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

      var mediaItems = _core.MainWindowM.IsFullScreen
        ? _core.MediaItemsM.Current == null
          ? Array.Empty<MediaItemM>()
          : new[] { _core.MediaItemsM.Current }
        : _core.ThumbnailsGridsM.Current == null
          ? Array.Empty<MediaItemM>()
          : _core.ThumbnailsGridsM.Current.GetSelectedOrAll().ToArray();

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
