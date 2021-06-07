using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;

namespace PictureManager.UserControls {
  public partial class ThumbnailsTabs {

    public ThumbnailsTabs() {
      InitializeComponent();
      UpdateTabMaxHeight();
    }

    public void AddTab(ThumbnailsGrid grid) {
      var tabItem = new TabItem {
        DataContext = grid,
        Content = new ThumbnailsGridControl {
          Name = "ThumbsGridControl"
        },
        HeaderTemplate = Tabs.FindResource("ThumbsTabItemTemplate") as DataTemplate
      };

      Tabs.SelectedIndex = Tabs.Items.Add(tabItem);

      UpdateTabMaxHeight();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      var grid = (ThumbnailsGrid)((FrameworkElement)((TabControl)sender).SelectedItem).DataContext;
      App.Core.Model.MediaItems.ThumbsGrid = grid;
      grid.UpdateSelected();
      App.Core.AppInfo.CurrentMediaItem = grid.Current;
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
      UpdateTabMaxHeight();
    }

    private void BtnCloseTab(object sender, RoutedEventArgs e) {
      if (!(((FrameworkElement)sender).DataContext is ThumbnailsGrid grid)) return;
      App.Core.MediaItemsViewModel.RemoveThumbnailsGrid(Tabs, grid);

      // set new SelectedItem and remove TabItem
      var tab = ((FrameworkElement)sender).TryFindParent<TabItem>();
      if (tab == null) return;
      var i = Tabs.Items.IndexOf(tab);
      Tabs.SelectedItem = Tabs.Items[i != 0 ? 0 : 1];
      Tabs.Items.Remove(tab);

      UpdateTabMaxHeight();
    }

    private void BtnAddTab(object sender, RoutedEventArgs e) {
      AddTab(App.Core.MediaItemsViewModel.AddThumbnailsGridModel());
    }

    private void UpdateTabMaxHeight() {
      var maxHeight = ActualHeight / Tabs.Items.Count;

      foreach (var tabItem in Tabs.Items.Cast<TabItem>())
        tabItem.MaxHeight = maxHeight;
    }

    private void Refresh(object sender, RoutedEventArgs e) {
      App.Core.Model.MediaItems.ThumbsGrid.ReloadFilteredItems();
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
    }
  }
}
