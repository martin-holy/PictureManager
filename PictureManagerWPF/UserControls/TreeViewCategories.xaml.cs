using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MH.UI.WPF.Interfaces;

namespace PictureManager.UserControls {
  public partial class TreeViewCategories {
    public TreeViewCategories() {
      InitializeComponent();
      TreeViewItemsEvents();
    }

    private static void TreeViewItemsEvents() {
      /*CatTreeViewUtils.OnAfterItemRename += async (o, e) => {
        if (o is FolderTreeVM { IsSelected: true } folder) {
          // reload if the folder was selected before
          await App.Ui.TreeView_Select(folder, false, false, false);
        }
      };*/

      /*CatTreeViewUtils.OnAfterItemDelete += (o, e) => {
        if (o is FolderTreeVM folder && Directory.Exists(folder.Model.FullPath)) {
          // delete folder, sub folders and mediaItems from file system
          AppCore.FileOperationDelete(new List<string> { folder.Model.FullPath }, true, false);
        }
      };*/

      /*CatTreeViewUtils.OnAfterOnDrop += async (o, e) => {
        var data = (object[])o;
        var src = data[0];
        var dest = data[1] as ICatTreeViewItem;
        //var aboveDest = (bool) data[2];
        var copy = (bool)data[3];
        var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

        switch (src) {
          case FolderTreeVM srcData: // Folder
            FoldersTreeVM.CopyMove(foMode, srcData.Model, ((FolderTreeVM)dest)?.Model);
            App.Core.MediaItems.DataAdapter.IsModified = true;
            App.Core.FoldersM.DataAdapter.IsModified = true;
            App.Core.FolderKeywordsM.Load();

            // reload last selected source if was moved
            if (foMode == FileOperationMode.Move && srcData.IsSelected) {
              var folder = ((FolderTreeVM)dest)?.Model.GetByPath(srcData.Model.Name);
              if (folder == null) return;
              CatTreeViewUtils.ExpandTo((FolderTreeVM)dest);
              await App.Ui.TreeView_Select((FolderTreeVM)dest, false, false, false);
            }

            break;

          case string[]: // MediaItems
            App.Ui.MediaItemsViewModel.CopyMove(foMode,
              App.Core.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList(),
              ((FolderTreeVM)dest)?.Model);
            App.Core.MediaItems.DataAdapter.IsModified = true;

            break;
        }

        App.Ui.MarkUsedKeywordsAndPeople();
      };*/

      /*CatTreeViewUtils.OnAfterSort += (o, e) => {
        // TODO tohle musi bejt jinak. setridim to v Modelu a ten to reportuje to ViewModelu
        // sort items in DB (items in root are already sorted from CatTreeViewUtils.Sort)
        if (o is not ICatTreeViewItem root) return;
        if (CatTreeViewUtils.GetTopParent(root) is not ICatTreeViewCategory cat || cat is not ITable table) return;

        // sort groups
        var groups = root.Items.OfType<ICatTreeViewGroup>().ToArray();
        foreach (var group in groups)
          App.Core.CategoryGroups.All.Remove(group as IRecord);
        foreach (var group in groups)
          App.Core.CategoryGroups.All.Add(group as IRecord);
        if (groups.Length != 0)
          App.Core.CategoryGroups.DataAdapter.IsModified = true;

        // sort items
        var items = root.Items.Where(x => x is not ICatTreeViewGroup).ToArray();
        foreach (var item in items)
          table.All.Remove(item as IRecord);
        foreach (var item in items)
          table.All.Add(item as IRecord);
        if (items.Length != 0)
          table.DataAdapter.IsModified = true;
      };*/
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
