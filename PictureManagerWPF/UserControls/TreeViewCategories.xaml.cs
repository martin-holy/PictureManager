using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager.UserControls {
  public partial class TreeViewCategories {
    public TreeView TreeView => TvCategories;

    public TreeViewCategories() {
      InitializeComponent();
      TreeViewItemsEvents();
    }

    private static void TreeViewItemsEvents() {
      CatTreeViewUtils.OnAfterItemRename += delegate(object sender, EventArgs args) {
        if (sender is Folder folder) {
          // reload if the folder was selected before
          if (folder.IsSelected)
            App.Core.TreeView_Select(folder, false, false, false);
        }
      };

      CatTreeViewUtils.OnAfterItemDelete += delegate (object sender, EventArgs args) {
        if (sender is Folder folder) {
          // delete folder, subfolders and mediaItems from file system
          if (Directory.Exists(folder.FullPath))
            AppCore.FileOperationDelete(new List<string> { folder.FullPath }, true, false);
        }
      };

      CatTreeViewUtils.OnAfterOnDrop += delegate(object sender, EventArgs args) {
        var data = (object[]) sender;
        var src = data[0];
        var dest = data[1] as ICatTreeViewItem;
        //var aboveDest = (bool) data[2];
        var copy = (bool) data[3];

        switch (src) {
          case Folder srcData: { // Folder
            var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

            FoldersViewModel.CopyMove(foMode, srcData, (Folder) dest);
            App.Core.Model.MediaItems.Helper.IsModified = true;
            App.Core.Model.Folders.Helper.IsModified = true;
            App.Core.Model.FolderKeywords.Load();

            // reload last selected source if was moved
            if (foMode == FileOperationMode.Move && srcData.IsSelected) {
              var folder = ((Folder) dest)?.GetByPath(srcData.Title);
              if (folder == null) return;
              CatTreeViewUtils.ExpandTo(folder);
              App.Core.TreeView_Select(folder, false, false, false);
            }

            break;
          }
          case string[] _: { // MediaItems
            var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;
            App.Core.MediaItemsViewModel.CopyMove(
              foMode, App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList(), (Folder)dest);
            App.Core.Model.MediaItems.Helper.IsModified = true;

            break;
          }
        }

        App.Core.Model.Sdb.SaveAllTables();
        App.Core.Model.MarkUsedKeywordsAndPeople();
      };
    }

    private void BtnNavCategory_OnClick(object sender, RoutedEventArgs e) {
      TvCategories.ScrollTo((ICatTreeViewItem)((Button)sender).DataContext);
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      e.Handled = true;

      var b = Extensions.FindThisOrParent<Border>(e.OriginalSource as FrameworkElement, "Border");

      if (b == null || b.ContextMenu?.Tag is bool) return;

      var item = b.DataContext;
      var menu = b.ContextMenu;
      var firstItem = true;
      var binding = new Binding(nameof(ContextMenu.PlacementTarget)) {
        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1)
      };

      if (menu == null) menu = new ContextMenu();
      menu.Tag = true; // indicates that menu has been fully created

      void AddMenuItem(ICommand command) {
        if (firstItem) {
          if (menu.Items.Count > 0)
            menu.Items.Add(new Separator());
          firstItem = false;
        }

        var menuItem = new MenuItem { Command = command, CommandParameter = item };
        menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);
        menu.Items.Add(menuItem);
      }

      if (CatTreeViewUtils.GetTopParent(item as ICatTreeViewItem) is ICatTreeViewCategory category) {
        if ((item as ICatTreeViewCategory)?.Category == Category.GeoNames)
          AddMenuItem(TreeViewCommands.GeoNameNewCommand);

        if (category.CanDeleteItems && (category.CanHaveSubItems || item is ICatTreeViewCategory || item is ICatTreeViewGroup)
            && (category.Category == Category.People || category.Category == Category.Keywords))
          AddMenuItem(TreeViewCommands.TagItemDeleteNotUsedCommand);
      }

      switch (item) {
        case Folder folder: {
            if (!(folder.Parent is ICatTreeViewCategory))
              AddMenuItem(TreeViewCommands.FolderAddToFavoritesCommand);

            AddMenuItem(TreeViewCommands.FolderSetAsFolderKeywordCommand);
            AddMenuItem(MetadataCommands.Reload2Command);
            AddMenuItem(MediaItemsCommands.RebuildThumbnailsCommand);
            break;
          }
        case Viewer _: {
            AddMenuItem(TreeViewCommands.ViewerIncludeFolderCommand);
            AddMenuItem(TreeViewCommands.ViewerExcludeFolderCommand);
            break;
          }
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _: {
            AddMenuItem(TreeViewCommands.LoadByTagCommand);
            AddMenuItem(TreeViewCommands.ActivateFilterAndCommand);
            AddMenuItem(TreeViewCommands.ActivateFilterOrCommand);
            AddMenuItem(TreeViewCommands.ActivateFilterNotCommand);
            break;
          }
        case FolderKeywords _: {
            AddMenuItem(WindowCommands.OpenFolderKeywordsListCommand);
            break;
          }
      }

      if (menu.Items.Count > 0 && b.ContextMenu == null)
        b.ContextMenu = menu;

      if (b.ContextMenu?.Items.Count == 0)
        b.ContextMenu = null;
    }

    private void TreeView_Select(object sender, MouseButtonEventArgs e) {
      /*
       SHIFT key => recursive
       (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide
       (Person, Keyword, GeoName)(filters) => MBL => or, MBL+ctrl => and, MBL+alt => hide
       (Rating)(filter) => MBL => OR between ratings, AND in files
       */
      e.Handled = true;
      if (e.OriginalSource is ToggleButton) return;
      App.Core.TreeView_Select(((TreeViewItem)sender).DataContext as ICatTreeViewItem,
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      App.Core.Model.MediaItemSizes.Size.SliderChanged = true;
      App.Core.MediaItemsViewModel.ReapplyFilter();
    }

    private void ShowSearch(object sender, RoutedEventArgs e) {
      Search.TbSearch.Text = string.Empty;
      Search.Visibility = Visibility.Visible;
    }
  }
}
