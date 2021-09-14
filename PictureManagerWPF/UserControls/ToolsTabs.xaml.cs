using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.UserControls {
  public partial class ToolsTabs : UserControl {
    public ToolsTabs() {
      InitializeComponent();
      AttachEvents();
    }

    private void AttachEvents() {
      App.Core.People.PropertyChanged += (o, e) => {
        if (e.PropertyName.Equals(nameof(App.Core.People.Current))) {
          var current = App.Core.People.Current;
          Activate(TabPerson, current != null);
          _ = PersonControl.ReloadPersonSegmentsAsync(current);

          if (current != null && !App.WMain.RightSlidePanel.IsOpen)
            App.WMain.RightSlidePanel.IsOpen = true;
        }
      };
    }

    private void BtnCloseTab_Click(object sender, RoutedEventArgs e) {
      if (Tabs.SelectedItem is TabItem tab) {
        if (tab == TabPerson) {
          App.Core.People.Current = null;
          App.Core.People.DeselectAll();
        }
        else
          tab.Visibility = Visibility.Collapsed;
      }

      Tabs.SelectedItem = Tabs.Items.Cast<TabItem>().FirstOrDefault(x => x.Visibility == Visibility.Visible);
      if (Tabs.SelectedItem == null)
        App.WMain.RightSlidePanel.IsOpen = false;
    }

    public void Activate(TabItem tabItem, bool activate) {
      tabItem.Visibility = activate ? Visibility.Visible : Visibility.Collapsed;
      if (activate)
        tabItem.IsSelected = true;
      else {
        var tab = Tabs.Items.Cast<TabItem>().FirstOrDefault(x => x.Visibility == Visibility.Visible);
        if (tab != null) tab.IsSelected = true;
      }
    }
  }
}
