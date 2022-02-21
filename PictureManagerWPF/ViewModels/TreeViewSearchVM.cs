using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.UI.WPF.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class TreeViewSearchVM : ObservableObject {
    private readonly TreeViewCategoriesVM _treeViewCategoriesVM;
    private string _searchText;
    private bool _isVisible;

    public string SearchText {
      get => _searchText;
      set {
        _searchText = value;
        OnPropertyChanged();
        Search();
      }
    }

    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }

    public ObservableCollection<TreeViewSearchItemVM> SearchResult { get; } = new();
    public RelayCommand<TreeViewSearchItemVM> NavigateToCommand { get; }
    public RelayCommand<object> CloseCommand { get; }

    public TreeViewSearchVM(TreeViewCategoriesVM treeViewCategoriesVM) {
      _treeViewCategoriesVM = treeViewCategoriesVM;
      NavigateToCommand = new(NavigateTo);
      CloseCommand = new(() => IsVisible = false);
    }

    private void NavigateTo(TreeViewSearchItemVM item) {
      if (item != null) {
        CatTreeView.ExpandTo(item.Item);
        _treeViewCategoriesVM.TvCategories.ScrollTo(item.Item);
      }

      IsVisible = false;
    }

    private void Search() {
      SearchResult.Clear();
      if (SearchText.Equals(string.Empty)) return;

      void AddToSearchResult(IEnumerable<TreeViewSearchItemVM> items) {
        foreach (var searchItem in items.OrderBy(x => x.Title))
          SearchResult.Add(searchItem);
      }

      // People
      AddToSearchResult(App.Core.PeopleM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new TreeViewSearchItemVM("IconPeople", x.Name, (x.Parent as CategoryGroupM)?.Name, _treeViewCategoriesVM.PeopleTreeVM.All[x.Id])));

      // Keywords
      AddToSearchResult(App.Core.KeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new TreeViewSearchItemVM("IconTag", x.Name, x.FullName, _treeViewCategoriesVM.KeywordsTreeVM.All[x.Id])));

      // GeoNames
      AddToSearchResult(App.Core.GeoNamesM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new TreeViewSearchItemVM("IconLocationCheckin", x.Name, x.FullName, _treeViewCategoriesVM.GeoNamesTreeVM.All[x.Id])));

      // Folders
      var result = App.Core.FoldersM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) && App.Core.ViewersM.CanViewerSee(x))
        .Select(x => new TreeViewSearchItemVM("IconFolder", x.Name, x.FullPath, _treeViewCategoriesVM.FoldersTreeVM.All[x.Id])).ToList();

      // Folder Keywords
      result.AddRange(App.Core.FolderKeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) && x.Folders.All(f => App.Core.ViewersM.CanViewerSee(f)))
        .Select(x => new TreeViewSearchItemVM("IconFolderPuzzle", x.Name, x.FullPath, _treeViewCategoriesVM.FolderKeywordsTreeVM.All[x.Id])));
      AddToSearchResult(result);
    }
  }
}
