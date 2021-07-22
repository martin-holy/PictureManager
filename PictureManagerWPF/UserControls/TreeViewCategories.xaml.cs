﻿using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class TreeViewCategories {
    public TreeViewCategories() {
      InitializeComponent();
      TreeViewItemsEvents();
    }

    private static void TreeViewItemsEvents() {
      CatTreeViewUtils.OnAfterItemRename += (o, e) => {
        if (o is Folder folder) {
          // reload if the folder was selected before
          if (folder.IsSelected)
            App.Ui.TreeView_Select(folder, false, false, false);
        }
      };

      CatTreeViewUtils.OnAfterItemDelete += (o, e) => {
        if (o is Folder folder) {
          // delete folder, subfolders and mediaItems from file system
          if (Directory.Exists(folder.FullPath))
            AppCore.FileOperationDelete(new List<string> { folder.FullPath }, true, false);
        }
      };

      CatTreeViewUtils.OnAfterOnDrop += (o, e) => {
        var data = (object[])o;
        var src = data[0];
        var dest = data[1] as ICatTreeViewItem;
        //var aboveDest = (bool) data[2];
        var copy = (bool)data[3];

        switch (src) {
          case Folder srcData: { // Folder
            var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

            FoldersViewModel.CopyMove(foMode, srcData, (Folder)dest);
            App.Db.SetModified<MediaItems>();
            App.Db.SetModified<Folders>();
            App.Core.FolderKeywords.Load();

            // reload last selected source if was moved
            if (foMode == FileOperationMode.Move && srcData.IsSelected) {
              var folder = ((Folder)dest)?.GetByPath(srcData.Title);
              if (folder == null) return;
              CatTreeViewUtils.ExpandTo(folder);
              App.Ui.TreeView_Select(folder, false, false, false);
            }

            break;
          }
          case string[] _: { // MediaItems
            var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;
            App.Ui.MediaItemsViewModel.CopyMove(
              foMode, App.Core.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList(), (Folder)dest);
            App.Db.SetModified<MediaItems>();

            break;
          }
        }

        App.Core.MarkUsedKeywordsAndPeople();
      };

      CatTreeViewUtils.OnAfterSort += (o, e) => {
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
          App.Db.SetModified<CategoryGroups>();

        // sort items
        var items = root.Items.Where(x => x is not ICatTreeViewGroup).ToArray();
        foreach (var item in items)
          table.All.Remove(item as IRecord);
        foreach (var item in items)
          table.All.Add(item as IRecord);
        if (items.Length != 0) {
          App.Db.Changes++;
          table.Helper.IsModified = true;
        }
      };
    }

    private void BtnNavCategory_OnClick(object sender, RoutedEventArgs e) =>
      TvCategories.ScrollTo((ICatTreeViewItem)((Button)sender).DataContext);

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
      App.Ui.TreeView_Select(((TreeViewItem)sender).DataContext as ICatTreeViewItem,
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      App.Core.MediaItemSizes.Size.SliderChanged = true;
      App.Ui.MediaItemsViewModel.ReapplyFilter();
    }

    private void ShowSearch(object sender, RoutedEventArgs e) {
      Search.TbSearch.Text = string.Empty;
      Search.Visibility = Visibility.Visible;
    }

    private void ItemToolTip_ToolTipOpening(object sender, ToolTipEventArgs e) =>
      (((FrameworkElement)sender).DataContext as Person)?.Face?.SetPictureAsync(App.Core.Faces.FaceSize);
  }
}
