﻿using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Common.ViewModels;

public sealed class TreeViewSearchVM : ObservableObject {
  private readonly TreeViewCategoriesVM _tvc;
  private string _searchText;
  private bool _isOpen;

  public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); Search(); } }
  public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
  public ObservableCollection<TreeViewSearchItemM> SearchResult { get; } = [];

  public RelayCommand OpenCommand { get; }
  public RelayCommand CloseCommand { get; }
  public RelayCommand<TreeViewSearchItemM> NavigateToCommand { get; }

  public TreeViewSearchVM(TreeViewCategoriesVM tvc) {
    _tvc = tvc;
    OpenCommand = new(Open, Res.IconMagnify, "Search");
    CloseCommand = new(() => IsOpen = false, MH.UI.Res.IconXCross, "Close");
    NavigateToCommand = new(NavigateTo);
  }

  private void NavigateTo(TreeViewSearchItemM item) {
    if (item == null) return;
    _tvc.Select(item.Category.TreeView);
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