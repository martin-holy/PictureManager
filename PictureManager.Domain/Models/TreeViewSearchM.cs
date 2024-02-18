using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchM : ObservableObject {
    private string _searchText;
    private bool _isOpen;
    private TreeViewSearchItemM _selected;

    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); Search(); } }
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public TreeViewSearchItemM Selected { get => _selected; set { _selected = value; OnPropertyChanged(); NavigateTo(value); } }
    public ObservableCollection<TreeViewSearchItemM> SearchResult { get; } = new();

    public RelayCommand OpenCommand { get; }
    public RelayCommand CloseCommand { get; }

    public TreeViewSearchM() {
      OpenCommand = new(Open, Res.IconMagnify, "Search");
      CloseCommand = new(() => IsOpen = false, Res.IconXCross, "Close");
    }

    private void NavigateTo(TreeViewSearchItemM item) {
      if (item == null) return;
      Core.VM.MainWindow.TreeViewCategories.Select(item.Category.TreeView);
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
      AddToSearchResult(Core.R.Person.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconPeople, x.Name, x, (x.Parent as CategoryGroupM)?.Name, Core.R.Person.Tree)));

      // Keywords
      AddToSearchResult(Core.R.Keyword.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconTag, x.Name, x, x.FullName, Core.R.Keyword.Tree)));

      // GeoNames
      AddToSearchResult(Core.R.GeoName.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
        .Select(x => new TreeViewSearchItemM(Res.IconLocationCheckin, x.Name, x, x.FullName, Core.R.GeoName.Tree)));

      // Folders
      var result = Core.R.Folder.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && Core.S.Viewer.CanViewerSee(x))
        .Select(x => new TreeViewSearchItemM(Res.IconFolder, x.Name, x, x.FullPath, Core.R.Folder.Tree)).ToList();

      // Folder Keywords
      result.AddRange(Core.R.FolderKeyword.All2
        .Where(x => x.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                && x.Folders.All(f => Core.S.Viewer.CanViewerSee(f)))
        .Select(x => new TreeViewSearchItemM(Res.IconFolderPuzzle, x.Name, x, x.FullPath, Core.R.FolderKeyword.Tree)));
      AddToSearchResult(result);
    }
  }
}
