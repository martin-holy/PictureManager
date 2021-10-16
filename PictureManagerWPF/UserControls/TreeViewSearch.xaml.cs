using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class TreeViewSearch : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

      var sep = Path.DirectorySeparatorChar.ToString();

      // People
      AddToSearchResult(App.Core.PeopleM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem(IconName.People, x.Name, (x.Parent as CategoryGroupM)?.Name, App.Ui.PeopleTreeVM.All[x.Id])));

      // Keywords
      AddToSearchResult(App.Core.KeywordsM.All
        .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem(IconName.Tag, x.Name, x.FullName, App.Ui.KeywordsTreeVM.All[x.Id])));

      // GeoNames
      AddToSearchResult(App.Core.GeoNames.All.Cast<GeoName>()
        .Where(x => x.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem(IconName.LocationCheckin, x.Title, CatTreeViewUtils.GetFullPath(x, sep), x)));

      // Folders
      var result = App.Core.Folders.All.Cast<Folder>()
        .Where(x => x.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) && !x.IsThisOrParentHidden())
        .Select(x => new SearchItem(IconName.Folder, x.Title, x.FullPath, x)).ToList();

      // Folder Keywords
      result.AddRange(App.Core.FolderKeywords.All
        .Where(x => x.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .Select(x => new SearchItem(IconName.FolderPuzzle, x.Title, CatTreeViewUtils.GetFullPath(x, sep), x)));
      AddToSearchResult(result);
    }

    private void NavigateTo(ICatTreeViewItem item) {
      if (item != null) {
        CatTreeViewUtils.ExpandTo(item);
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
    public IconName IconName { get; set; }
    public string Title { get; set; }
    public string ToolTip { get; set; }
    public ICatTreeViewItem Item { get; set; }

    public SearchItem(IconName iconName, string title, string toolTip, ICatTreeViewItem item) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
    }
  }
}
