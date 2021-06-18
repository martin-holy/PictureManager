using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace PictureManager.UserControls {
  public partial class MainTabs {
    public EventHandler OnTabItemClose;
    public Type SelectedTabItemContentType { get; set; }

    public MainTabs() {
      InitializeComponent();
      UpdateTabMaxHeight();

      Tabs.SelectionChanged += (o, e) => {
        SelectedTabItemContentType = ((TabItem) Tabs.SelectedItem)?.Content?.GetType();
      };
    }

    public void SetTab(object dataContext, object content) {
      if (Tabs.SelectedItem is TabItem tab) {
        OnTabItemClose?.Invoke(tab.DataContext, null);

        tab.HeaderTemplate = Tabs.FindResource("ThumbsTabItemTemplate") as DataTemplate;
        tab.DataContext = dataContext;
        tab.Content = content;
      }

      UpdateTabMaxHeight();
    }

    public void AddTab() {
      var tabItem = new TabItem {
        HeaderTemplate = Tabs.FindResource("ThumbsTabItemTemplate") as DataTemplate
      };

      Tabs.SelectedIndex = Tabs.Items.Add(tabItem);

      SetAddTabButton();
    }

    private void BtnAddTab_Click(object sender, RoutedEventArgs e) => AddTab();

    private void BtnCloseTab_Click(object sender, RoutedEventArgs e) {
      if (!(sender is FrameworkElement elm)) return;

      OnTabItemClose?.Invoke(elm.DataContext, null);

      Tabs.Items.Remove(elm.TryFindParent<TabItem>());

      SetAddTabButton();
    }

    private void SetAddTabButton() {
      if (Tabs.Items.Count == 0) AddTab();

      // Tag == true => show add tab button
      ((TabItem) Tabs.Items[0]).Tag = true;

      UpdateTabMaxHeight();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateTabMaxHeight();

    private void UpdateTabMaxHeight() {
      var maxHeight = ActualHeight / Tabs.Items.Count;

      foreach (var tabItem in Tabs.Items.Cast<TabItem>())
        tabItem.MaxHeight = maxHeight;
    }

    //TODO
    private void Refresh(object sender, RoutedEventArgs e) {
      App.Core.MediaItems.ThumbsGrid?.ReloadFilteredItems();
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }
  }
}
