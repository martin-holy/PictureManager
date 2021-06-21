using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;

namespace PictureManager.UserControls {
  public partial class MainTabs {
    public EventHandler OnTabItemClose;

    public MainTabs() {
      InitializeComponent();
      UpdateTabMaxHeight();
    }

    public bool IsThisContentSet(Type type) => ((FrameworkElement) ((TabItem) Tabs.SelectedItem).Content)?.GetType() == type;

    public void SetTab(object dataContext, object content, ContextMenu contextMenu) {
      if (Tabs.SelectedItem is TabItem tab) {
        OnTabItemClose?.Invoke(tab, null);

        tab.DataContext = dataContext;
        tab.Content = content;
        tab.ContextMenu = contextMenu;
        BindingOperations.SetBinding(tab, HeaderedContentControl.HeaderProperty, new Binding("Title"));
      }

      UpdateTabMaxHeight();
    }

    public void AddTab() {
      Tabs.SelectedIndex = Tabs.Items.Add(new TabItem());

      SetAddTabButton();
    }

    private void BtnAddTab_Click(object sender, RoutedEventArgs e) => AddTab();

    private void BtnCloseTab_Click(object sender, RoutedEventArgs e) {
      if (!(sender is FrameworkElement elm)) return;
      var tab = elm.TryFindParent<TabItem>();
      OnTabItemClose?.Invoke(tab, null);
      Tabs.Items.Remove(tab);
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
  }
}
