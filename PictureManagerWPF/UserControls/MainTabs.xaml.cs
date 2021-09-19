using MahApps.Metro.Controls;
using PictureManager.Domain;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PictureManager.UserControls {
  public partial class MainTabs {
    public EventHandler OnTabItemClose { get; set; }
    public Action OnAddTab { get; set; }

    public MainTabs() {
      InitializeComponent();
      SetHeaderMargin();
      UpdateTabMaxHeight();
    }

    public bool IsThisContentSet(Type type) => GetSelectedContent()?.GetType() == type;

    public object GetSelectedContent() => ((TabItem)Tabs.SelectedItem)?.Content;

    public TabItem GetTabWithContentTypeOf(Type type) =>
      Tabs.Items.Cast<TabItem>().FirstOrDefault(x => x.Content?.GetType() == type);

    public T ActivateTab<T>(IconName iconName) where T : new() {
      var tab = GetTabWithContentTypeOf(typeof(T)) ?? AddTab(iconName, new T());

      if (!tab.IsSelected)
        tab.IsSelected = true;

      return (T)tab.Content;
    }

    public TabItem AddTab(IconName iconName, object content) {
      var tab = new TabItem {
        Tag = iconName,
        DataContext = (content as FrameworkElement)?.DataContext,
        Content = content
      };

      BindingOperations.SetBinding(tab, HeaderedContentControl.HeaderProperty, new Binding("Title"));
      Tabs.Items.Add(tab);
      UpdateTabMaxHeight();

      return tab;
    }

    private void BtnAddTab_Click(object sender, RoutedEventArgs e) => OnAddTab?.Invoke();

    private void BtnCloseTab_Click(object sender, RoutedEventArgs e) {
      if (sender is not FrameworkElement elm) return;
      var tab = elm.TryFindParent<TabItem>();
      OnTabItemClose?.Invoke(tab, EventArgs.Empty);
      Tabs.Items.Remove(tab);
      UpdateTabMaxHeight();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateTabMaxHeight();

    private void UpdateTabMaxHeight() {
      var maxHeight = ActualHeight / Tabs.Items.Count;

      foreach (var tabItem in Tabs.Items.Cast<TabItem>())
        tabItem.MaxHeight = maxHeight;
    }

    private void SetHeaderMargin() {
      Tabs.ApplyTemplate();
      if (Tabs.Template.FindName("HeaderPanel", Tabs) is FrameworkElement panel)
        panel.Margin = new Thickness(0, 26, 0, 0);
    }
  }
}
