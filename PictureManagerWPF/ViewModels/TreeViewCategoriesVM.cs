using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class TreeViewCategoriesVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private bool _isPinned = true;
    private bool _isOpen = true;
    private bool _isPinnedInViewer;
    private bool _isPinnedInBrowser = true;

    public CategoryGroupsTreeVM CategoryGroupsTreeVM { get; }
    public DrivesTreeVM DrivesTreeVM { get; }
    public FavoriteFoldersTreeVM FavoriteFoldersTreeVM { get; }
    public FoldersTreeVM FoldersTreeVM { get; }
    public RatingsTreeVM RatingsTreeVM { get; }
    public MediaItemSizesTreeVM MediaItemSizesTreeVM { get; }
    public PeopleTreeVM PeopleTreeVM { get; }
    public FolderKeywordsTreeVM FolderKeywordsTreeVM { get; }
    public KeywordsTreeVM KeywordsTreeVM { get; }
    public GeoNamesTreeVM GeoNamesTreeVM { get; }
    public ViewersTreeVM ViewersTreeVM { get; }
    public ObservableCollection<CatTreeViewCategoryBase> Items { get; }

    public TreeViewSearchVM TreeViewSearchVM { get; }
    public CatTreeView TvCategories { get; }
    public Dictionary<object, int> MarkedTags { get; } = new();
    
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public bool IsPinned {
      get => _isPinned;
      set {
        _isPinned = value;
        IsOpen = value;
        OnPropertyChanged();
      }
    }
    
    public RelayCommand<object> ShowSearchCommand { get; }
    public RelayCommand<ICatTreeViewItem> ScrollToCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }
    public RelayCommand<ICatTreeViewItem> TagItemDeleteNotUsedCommand { get; }
    public RelayCommand<object> ToggleIsPinnedCommand { get; }

    public TreeViewCategoriesVM(Core core, AppCore coreVM) {
      _core = core;
      _coreVM = coreVM;

      CategoryGroupsTreeVM = new(_core.CategoryGroupsM);
      DrivesTreeVM = new();
      TreeViewSearchVM = new(this);
      TvCategories = new();

      #region Categories
      FavoriteFoldersTreeVM = new(_core.FavoriteFoldersM);
      FoldersTreeVM = new(_core, _coreVM, _core.FoldersM, DrivesTreeVM);
      RatingsTreeVM = new();
      MediaItemSizesTreeVM = new(_coreVM.ThumbnailsGridsVM);
      PeopleTreeVM = new(_core.PeopleM, CategoryGroupsTreeVM);
      FolderKeywordsTreeVM = new(_core.FolderKeywordsM, _core.FoldersM);
      KeywordsTreeVM = new(_core.KeywordsM, CategoryGroupsTreeVM);
      GeoNamesTreeVM = new(_core.GeoNamesM);
      ViewersTreeVM = new(_core.ViewersM);

      Items = new() { FavoriteFoldersTreeVM, FoldersTreeVM, RatingsTreeVM, MediaItemSizesTreeVM, PeopleTreeVM, FolderKeywordsTreeVM, KeywordsTreeVM, GeoNamesTreeVM, ViewersTreeVM };
      #endregion

      FoldersTreeVM.Load();

      ShowSearchCommand = new(ShowSearch);
      ScrollToCommand = new(ScrollTo);
      SelectCommand = new(Select);
      TagItemDeleteNotUsedCommand = new(TagItemDeleteNotUsed);
      ToggleIsPinnedCommand = new(ToggleIsPinned);

      FoldersTreeVM.OnAfterItemCreate += (o, _) =>
        TvCategories.ScrollTo((ICatTreeViewItem)o);
      PeopleTreeVM.OnAfterItemCreate += (o, _) =>
        TvCategories.ScrollTo((ICatTreeViewItem)o);
      KeywordsTreeVM.OnAfterItemCreate += (o, _) =>
        TvCategories.ScrollTo((ICatTreeViewItem)o);
      DrivesTreeVM.DriveExpandedChangedEvent += (_, e) =>
        FoldersTreeVM.HandleItemExpandedChanged(e.Data as FolderTreeVM);

      FoldersTreeVM.OnAfterItemRename += (o, _) => {
        // reload if the folder was selected before
        if (o is FolderTreeVM { IsSelected: true } folder)
          Select(folder);
      };
    }

    private void ToggleIsPinned() {
      if (_coreVM.MediaViewerVM.IsVisible)
        _isPinnedInViewer = !_isPinnedInViewer;
      else
        _isPinnedInBrowser = !_isPinnedInBrowser;
    }

    public void SetIsPinned(bool inViewer) =>
      IsPinned = inViewer
        ? _isPinnedInViewer
        : _isPinnedInBrowser;

    private void ShowSearch() {
      TreeViewSearchVM.SearchText = string.Empty;
      TreeViewSearchVM.IsVisible = true;
    }

    private void ScrollTo(ICatTreeViewItem item) =>
      TvCategories.ScrollTo(item);

    private void Select(MouseButtonEventArgs e) =>
      Select((e.OriginalSource as FrameworkElement)?.DataContext as ICatTreeViewItem);

    private void TagItemDeleteNotUsed(ICatTreeViewItem root) {
      if (MH.Utils.Tree.GetTopParent(root) is not CatTreeViewCategoryBase cat) return;
      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{cat.GetTitle(root)}'?", true)) return;

      switch (cat.Category) {
        case Category.People: 
          _core.PeopleM.DeleteNotUsed(
            root.Items.OfType<PersonTreeVM>().Select(x => x.Model),
            _core.MediaItemsM.All);
          break;

        case Category.Keywords:
          _core.KeywordsM.DeleteNotUsed(
            root.Items.OfType<KeywordTreeVM>().Select(x => x.Model),
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
          var vm = f.Parent is FoldersM
            ? DrivesTreeVM.All[f.Id]
            : FoldersTreeVM.All[f.Id];
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
        MarkedTagsAddWithIncrease(RatingsTreeVM.GetRatingByValue(mi.Rating));
      }

      OnPropertyChanged(nameof(MarkedTags));
    }

    public void Select(ICatTreeViewItem item) {
      // SHIFT key => recursive
      // (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide

      if (item == null) return;

      if (item is RatingTreeVM or PersonTreeVM or KeywordTreeVM or GeoNameTreeVM && _core.MediaItemsM.IsEditModeOn) {
        item.IsSelected = false;

        if (_coreVM.MediaItemsVM.SetMetadata(item) == 0) return;

        MarkUsedKeywordsAndPeople();
        if (item is RatingTreeVM)
          _coreVM.StatusPanelVM.UpdateRating();

        return;
      }

      switch (item) {
        case RatingTreeVM r:
          _ = _coreVM.ThumbnailsGridsVM.ActivateFilter(r, DisplayFilter.Or);
          break;

        case KeywordTreeVM k:
          App.Core.ToggleKeyword(k.Model);
          break;

        case PersonTreeVM p:
          SetPerson(p.Model);
          break;

        case FavoriteFolderTreeVM ff:
          if (!_core.FoldersM.IsFolderVisible(ff.Model.Folder)) break;
          var folderTreeVM = FoldersTreeVM.All[ff.Model.Folder.Id];
          CatTreeView.ExpandTo(folderTreeVM);
          TvCategories.ScrollTo(folderTreeVM);
          break;

        case FolderTreeVM:
        case FolderKeywordTreeVM:
          if (_coreVM.MediaViewerVM.IsVisible)
            _coreVM.MainWindowVM.IsFullScreen = false;

          var (and, hide, recursive) = InputUtils.GetControlAltShiftModifiers();
          _ = _coreVM.ThumbnailsGridsVM.LoadByFolder(item, and, hide, recursive);
          break;

        case ViewerTreeVM v:
          _coreVM.MainTabsVM.Activate(_coreVM.ViewerVM.MainTabsItem);
          _coreVM.ViewerVM.Viewer = v.Model;
          break;

        case ICatTreeViewCategory cat:
          if (cat is PeopleTreeVM) {
            _coreVM.MainTabsVM.Activate(_coreVM.PeopleVM.MainTabsItem);
            _core.PeopleM.ReloadPeopleInGroups();
          }
          break;
      }

      item.IsSelected = false;
    }

    private void SetPerson(PersonM person) {
      var sCount = _core.SegmentsM.Selected.Count;
      if (sCount == 0) return;

      var msgCount = sCount > 1 ? $"'s ({sCount})" : string.Empty;
      var msg = $"Do you want to set ({person.Name}) to selected segment{msgCount}??";

      if (!MessageDialog.Show("Set Person", msg, true)) return;
      _core.SegmentsM.SetSelectedAsPerson(person);
    }
  }
}
