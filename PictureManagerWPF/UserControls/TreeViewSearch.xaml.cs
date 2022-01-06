﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;

namespace PictureManager.UserControls {
  public partial class TreeViewSearch : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private string _searchText;

    public string SearchText {
      get => _searchText;
      set {
        _searchText = value;
        OnPropertyChanged();
        Search();
      }
    }

    public ObservableCollection<SearchItem> SearchResult { get; } = new();

    public TreeViewSearch() {
      InitializeComponent();
    }

    private void Search() {
      SearchResult.Clear();
      if (SearchText.Equals(string.Empty)) return;

      void AddToSearchResult(IEnumerable<SearchItem> items) {
        foreach (var searchItem in items.OrderBy(x => x.Title))
          SearchResult.Add(searchItem);
      }

      // People
      AddToSearchResult(App.Core.PeopleM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem("IconPeople", x.Name, (x.Parent as CategoryGroupM)?.Name, App.Ui.PeopleTreeVM.All[x.Id])));

      // Keywords
      AddToSearchResult(App.Core.KeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem("IconTag", x.Name, x.FullName, App.Ui.KeywordsTreeVM.All[x.Id])));

      // GeoNames
      AddToSearchResult(App.Core.GeoNamesM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem("IconLocationCheckin", x.Name, x.FullName, App.Ui.GeoNamesTreeVM.All[x.Id])));

      // Folders
      var result = App.Core.FoldersM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) && App.Core.ViewersM.CanViewerSee(x))
        .Select(x => new SearchItem("IconFolder", x.Name, x.FullPath, App.Ui.FoldersTreeVM.All[x.Id])).ToList();

      // Folder Keywords
      result.AddRange(App.Core.FolderKeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) && x.Folders.All(f => App.Core.ViewersM.CanViewerSee(f)))
        .Select(x => new SearchItem("IconFolderPuzzle", x.Name, x.FullPath, App.Ui.FolderKeywordsTreeVM.All[x.Id])));
      AddToSearchResult(result);
    }

    private void NavigateTo(ICatTreeViewItem item) {
      if (item != null) {
        CatTreeView.ExpandTo(item);
        App.WMain.TreeViewCategories.TvCategories.ScrollTo(item);
      }

      CloseSearch(null, null);
    }

    private void CloseSearch(object sender, RoutedEventArgs e) => Visibility = Visibility.Collapsed;

    private void TbSearch_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
      if (Visibility == Visibility.Visible)
        Keyboard.Focus(TbSearch);
    }

    private void SearchResult_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      var item = ((SearchItem)((ListBox)sender).SelectedItem)?.Item;
      NavigateTo(item);
    }
  }

  public class SearchItem {
    public string IconName { get; set; }
    public string Title { get; set; }
    public string ToolTip { get; set; }
    public ICatTreeViewItem Item { get; set; }

    public SearchItem(string iconName, string title, string toolTip, ICatTreeViewItem item) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
    }
  }
}
