﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.UserControls {
  public partial class TreeViewSearch: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _searchText;

    public string SearchText {
      get => _searchText;
      set {
        _searchText = value; 
        OnPropertyChanged();
        Search();
      }
    }

    public ObservableCollection<SearchItem> SearchResult { get; } = new ObservableCollection<SearchItem>();

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

      var sep = Path.DirectorySeparatorChar.ToString();

      // People
      AddToSearchResult(App.Core.Model.People.All
        .Where(x => x.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(x => new SearchItem(IconName.People, x.Title, (x.Parent as CategoryGroup)?.Title, x)));

      // Keywords
      AddToSearchResult(App.Core.Model.Keywords.All
        .Where(x => x.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(x => new SearchItem(IconName.Tag, x.Title, x.FullPath, x)));

      // GeoNames
      AddToSearchResult(App.Core.Model.GeoNames.All
        .Where(x => x.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(x => new SearchItem(IconName.LocationCheckin, x.Title, x.GetFullPath(sep), x)));

      // Folders
      var result = App.Core.Model.Folders.All
        .Where(x => x.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(x => new SearchItem(IconName.Folder, x.Title, x.FullPath, x)).ToList();

      // Folder Keywords
      result.AddRange(App.Core.Model.FolderKeywords.All
        .Where(x => x.Title.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(x => new SearchItem(IconName.FolderPuzzle, x.Title, x.GetFullPath(sep), x)));
      AddToSearchResult(result);
    }

    private void NavigateTo(object sender, SelectionChangedEventArgs e) {
      var item = ((SearchItem)((ListBox)sender).SelectedItem)?.Item;

      if (item != null) {
        BaseTreeViewItem.ExpandTo(item);
        App.WMain.TreeViewCategories.TreeItemBringIntoView(item);
      }

      CloseSearch(null, null);
    }

    private void CloseSearch(object sender, RoutedEventArgs e) {
      Visibility = Visibility.Collapsed;
    }

    private void TbSearch_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
      if (Visibility == Visibility.Visible) {
        Dispatcher.BeginInvoke((Action)delegate {
          Keyboard.Focus(TbSearch);
        }, DispatcherPriority.Render);
      }
    }
  }

  public class SearchItem {
    public IconName IconName { get; set; }
    public string Title { get; set; }
    public string ToolTip { get; set; }
    public BaseTreeViewItem Item { get; set; }

    public SearchItem(IconName iconName, string title, string toolTip, BaseTreeViewItem item) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
    }
  }
}
