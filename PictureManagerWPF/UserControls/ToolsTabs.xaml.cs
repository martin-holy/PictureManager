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
      App.Ui.PeopleBaseVM.PropertyChanged += (o, e) => {
        if (e.PropertyName.Equals(nameof(App.Ui.PeopleBaseVM.Current))) {
          var current = App.Ui.PeopleBaseVM.Current;
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
          App.Ui.PeopleBaseVM.Current = null;
          App.Ui.PeopleBaseVM.DeselectAll();
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
      tabItem.IsSelected = activate;
    }
  }
}
