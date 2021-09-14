using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PictureManager.UserControls {
  public partial class MainTabs {
    public EventHandler OnTabItemClose { get; set; }

    public MainTabs() {
      InitializeComponent();
      UpdateTabMaxHeight();
    }

    public bool IsThisContentSet(Type type) => GetSelectedContent()?.GetType() == type;

    public object GetSelectedContent() => ((TabItem)Tabs.SelectedItem).Content;

    public TabItem GetTabWithContentTypeOf(Type type) =>
      Tabs.Items.Cast<TabItem>().SingleOrDefault(x => x.Content?.GetType() == type);

    public T GetContentOfType<T>() where T : new() {
      var tab = GetTabWithContentTypeOf(typeof(T)) ?? AddTab();

      if (tab.Content is not T) {
        var control = new T();
        SetTab(control, control, null);
        return control;
      }

      tab.IsSelected = true;
      return (T)tab.Content;
    }

    public void SetTab(object dataContext, object content, ContextMenu contextMenu) {
      if (Tabs.SelectedItem is TabItem tab) {
        OnTabItemClose?.Invoke(tab, EventArgs.Empty);

        tab.DataContext = dataContext;
        tab.Content = content;
        var tabHeader = tab.FindChild<StackPanel>("TabHeader");
        if (tabHeader != null)
          tabHeader.ContextMenu = contextMenu;

        BindingOperations.SetBinding(tab, HeaderedContentControl.HeaderProperty, new Binding("Title"));
      }

      UpdateTabMaxHeight();
    }

    public TabItem AddTab() {
      var tab = Tabs.Items.Cast<TabItem>().SingleOrDefault(x => x.Content == null);
      if (tab == null) {
        tab = new TabItem();
        Tabs.Items.Add(tab);
        tab.UpdateLayout();
      }

      Tabs.SelectedItem = tab;
      SetAddTabButton();

      return tab;
    }

    private void BtnAddTab_Click(object sender, RoutedEventArgs e) => AddTab();

    private void BtnCloseTab_Click(object sender, RoutedEventArgs e) {
      if (sender is not FrameworkElement elm) return;
      var tab = elm.TryFindParent<TabItem>();
      OnTabItemClose?.Invoke(tab, EventArgs.Empty);
      Tabs.Items.Remove(tab);
      SetAddTabButton();
    }

    private void SetAddTabButton() {
      if (Tabs.Items.Count == 0) AddTab();

      // Tag == true => show add tab button
      ((TabItem)Tabs.Items[0]).Tag = true;

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
