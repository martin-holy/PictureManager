using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchM : ObservableObject {
    private readonly Core _core;
    private string _searchText;
    private bool _isVisible;

    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); Search(); } }
    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    public ObservableCollection<TreeViewSearchItemM> SearchResult { get; } = new();

    public RelayCommand<TreeViewSearchItemM> NavigateToCommand { get; }
    public RelayCommand<object> CloseCommand { get; }

    public TreeViewSearchM(Core core) {
      _core = core;

      NavigateToCommand = new(NavigateTo);
      CloseCommand = new(() => IsVisible = false);
    }

    public void NavigateTo(TreeViewSearchItemM item) {
      _core.TreeViewCategoriesM.SelectedCategory = item.Category;
      item.Category.ScrollTo(item.Item);
      IsVisible = false;
    }

    private void Search() {
      SearchResult.Clear();
      if (SearchText.Equals(string.Empty)) return;

      void AddToSearchResult(IEnumerable<TreeViewSearchItemM> items) {
        foreach (var searchItem in items.OrderBy(x => x.Title))
          SearchResult.Add(searchItem);
      }

      // People
      AddToSearchResult(_core.PeopleM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconPeople, x.Name, (x.Parent as CategoryGroupM)?.Name, x, _core.PeopleM)));

      // Keywords
      AddToSearchResult(_core.KeywordsM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconTag, x.Name, x.FullName, x, _core.KeywordsM)));

      // GeoNames
      AddToSearchResult(_core.GeoNamesM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconLocationCheckin, x.Name, x.FullName, x, _core.GeoNamesM)));

      // Folders
      var result = _core.FoldersM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && _core.ViewersM.CanViewerSee(x))
        .Select(x => new TreeViewSearchItemM(Res.IconFolder, x.Name, x.FullPath, x, _core.FoldersM)).ToList();

      // Folder Keywords
      result.AddRange(_core.FolderKeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && x.Folders.All(f => _core.ViewersM.CanViewerSee(f)))
        .Select(x => new TreeViewSearchItemM(Res.IconFolderPuzzle, x.Name, x.FullPath, x, _core.FolderKeywordsM)));
      AddToSearchResult(result);
    }
  }
}
