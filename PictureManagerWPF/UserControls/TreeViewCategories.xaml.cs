using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MH.UI.WPF.Interfaces;

namespace PictureManager.UserControls {
  public partial class TreeViewCategories {
    public TreeViewCategories() {
      InitializeComponent();

      App.Ui.FoldersTreeVM.OnAfterItemCreate += (o, _) => TvCategories.ScrollTo((ICatTreeViewItem)o);
      App.Ui.PeopleTreeVM.OnAfterItemCreate += (o, _) => TvCategories.ScrollTo((ICatTreeViewItem)o);
      App.Ui.KeywordsTreeVM.OnAfterItemCreate += (o, _) => TvCategories.ScrollTo((ICatTreeViewItem)o);
    }

    private void BtnNavCategory_OnClick(object sender, RoutedEventArgs e) =>
      TvCategories.ScrollTo((ICatTreeViewItem)((Button)sender).DataContext);

    private void TreeView_Select(object sender, MouseButtonEventArgs e) {
      /*
       SHIFT key => recursive
       (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide
       (Person, Keyword, GeoName)(filters) => MBL => or, MBL+ctrl => and, MBL+alt => hide
       (Rating)(filter) => MBL => OR between ratings, AND in files
       */
      e.Handled = true;
      if (e.OriginalSource is ToggleButton) return;
      _ = App.Ui.TreeView_Select(((TreeViewItem)sender).DataContext as ICatTreeViewItem);
    }

    private void ShowSearch(object sender, RoutedEventArgs e) {
      Search.TbSearch.Text = string.Empty;
      Search.Visibility = Visibility.Visible;
    }
  }
}
