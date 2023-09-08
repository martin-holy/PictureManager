using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchM : ObservableObject {
    private readonly Core _core;
    private string _searchText;
    private bool _isOpen;
    private TreeViewSearchItemM _selected;

    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); Search(); } }
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public TreeViewSearchItemM Selected { get => _selected; set { _selected = value; OnPropertyChanged(); NavigateTo(value); } }
    public ObservableCollection<TreeViewSearchItemM> SearchResult { get; } = new();

    public RelayCommand<object> OpenCommand { get; }
    public RelayCommand<object> CloseCommand { get; }

    public TreeViewSearchM(Core core) {
      _core = core;

      OpenCommand = new(Open);
      CloseCommand = new(() => IsOpen = false);
    }

    private void NavigateTo(TreeViewSearchItemM item) {
      if (item == null) return;
      _core.TreeViewCategoriesM.Select(item.Category.TreeView);
      item.Category.TreeView.ScrollTo((ITreeItem)item.Data);
      IsOpen = false;
    }

    private void Open() {
      SearchText = string.Empty;
      IsOpen = true;
    }

    private void Search() {
      SearchResult.Clear();
      if (SearchText.Equals(string.Empty)) return;

      void AddToSearchResult(IEnumerable<TreeViewSearchItemM> items) {
        foreach (var searchItem in items.OrderBy(x => x.Name))
          SearchResult.Add(searchItem);
      }

      // People
      AddToSearchResult(_core.PeopleM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconPeople, x.Name, x, (x.Parent as CategoryGroupM)?.Name, _core.PeopleM)));

      // Keywords
      AddToSearchResult(_core.KeywordsM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconTag, x.Name, x, x.FullName, _core.KeywordsM)));

      // GeoNames
      AddToSearchResult(_core.GeoNamesM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconLocationCheckin, x.Name, x, x.FullName, _core.GeoNamesM)));

      // Folders
      var result = _core.FoldersM.DataAdapter.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && _core.ViewersM.CanViewerSee(x))
        .Select(x => new TreeViewSearchItemM(Res.IconFolder, x.Name, x, x.FullPath, _core.FoldersM)).ToList();

      // Folder Keywords
      result.AddRange(_core.FolderKeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && x.Folders.All(f => _core.ViewersM.CanViewerSee(f)))
        .Select(x => new TreeViewSearchItemM(Res.IconFolderPuzzle, x.Name, x, x.FullPath, _core.FolderKeywordsM)));
      AddToSearchResult(result);
    }
  }
}
