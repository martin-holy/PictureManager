using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager.UserControls {
  public partial class TreeViewCategories {
    private Point _dragDropStartPosition;
    private object _dragDropObject;

    public TreeView TreeView => TvCategories;

    public TreeViewCategories() {
      InitializeComponent();
    }

    private void BtnNavCategory_OnClick(object sender, RoutedEventArgs e) {
      var cat = (BaseTreeViewItem)((Button)sender).DataContext;
      if (!(TvCategories.ItemContainerGenerator.ContainerFromItem(cat) is TreeViewItem tvi)) return;
      TvCategories.FindChildren<ScrollViewer>(true).SingleOrDefault()?.ScrollToBottom();
      tvi.BringIntoView();
    }

    //this is PreviewMouseRightButtonDown on StackPanel in TreeView
    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      var stackPanel = (StackPanel)sender;
      if (stackPanel.ContextMenu != null) return;

      var item = stackPanel.DataContext;
      var menu = new ContextMenu { Tag = item };
      var binding = new Binding("PlacementTarget") {
        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1)
      };

      void AddMenuItem(ICommand command) {
        var menuItem = new MenuItem { Command = command, CommandParameter = item };
        menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);
        menu.Items.Add(menuItem);
      }

      if ((item as BaseTreeViewItem)?.GetTopParent() is BaseCategoryItem category) {

        if (item is BaseCategoryItem && category.Category == Category.GeoNames)
          AddMenuItem(TreeViewCommands.GeoNameNewCommand);

        if (category.CanModifyItems) {
          var cat = item as BaseCategoryItem;
          var group = item as CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            AddMenuItem(TreeViewCommands.TagItemNewCommand);
            AddMenuItem(TreeViewCommands.TagItemDeleteNotUsedCommand);
          }

          if (item is BaseTreeViewTagItem && group == null || item is Viewer) {
            AddMenuItem(TreeViewCommands.TagItemRenameCommand);
            AddMenuItem(TreeViewCommands.TagItemDeleteCommand);
          }

          if (category.CanHaveGroups && cat != null)
            AddMenuItem(TreeViewCommands.CategoryGroupNewCommand);

          if (group != null) {
            AddMenuItem(TreeViewCommands.CategoryGroupRenameCommand);
            AddMenuItem(TreeViewCommands.CategoryGroupDeleteCommand);
          }
        }
      }

      switch (item) {
        case Folder folder: {
            AddMenuItem(TreeViewCommands.FolderNewCommand);

            if (folder.Parent != null) {
              AddMenuItem(TreeViewCommands.FolderRenameCommand);
              AddMenuItem(TreeViewCommands.FolderDeleteCommand);
              AddMenuItem(TreeViewCommands.FolderAddToFavoritesCommand);
            }

            AddMenuItem(TreeViewCommands.FolderSetAsFolderKeywordCommand);
            AddMenuItem(MetadataCommands.Reload2Command);
            AddMenuItem(MediaItemsCommands.RebuildThumbnailsCommand);
            break;
          }
        case FavoriteFolder _: {
            AddMenuItem(TreeViewCommands.FolderRemoveFromFavoritesCommand);
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
            AddMenuItem(TreeViewCommands.ActivateFilterAndCommand);
            AddMenuItem(TreeViewCommands.ActivateFilterOrCommand);
            AddMenuItem(TreeViewCommands.ActivateFilterNotCommand);
            break;
          }
        case FolderKeywords _: {
            AddMenuItem(Commands.WindowCommands.OpenFolderKeywordsListCommand);
            break;
          }
        case BaseTreeViewItem bti: {
            if (bti.Parent?.Parent is Viewer)
              AddMenuItem(TreeViewCommands.ViewerRemoveFolderCommand);
            break;
          }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void TreeView_Select(object sender, MouseButtonEventArgs e) {
      /*
       SHIFT key => recursive
       (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide
       (Person, Keyword, GeoName)(filters) => MBL => or, MBL+ctrl => and, MBL+alt => hide
       (Rating)(filter) => MBL => OR between ratings, AND in files
       */
      if (e.ChangedButton != MouseButton.Left) return;
      App.Core.TreeView_Select(((StackPanel)sender).DataContext as BaseTreeViewItem,
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private void TreeView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (!(sender is StackPanel stackPanel)) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TreeView_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      if (!(e.OriginalSource is StackPanel stackPanel) || _dragDropObject == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void TreeView_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel)sender;
      if (!(panel.DataContext is BaseTreeViewItem destData)) return;

      // MediaItems
      if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
        var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;
        App.Core.MediaItemsViewModel.CopyMove(
          foMode, App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList(), (Folder)destData);
        App.Core.Model.MediaItems.Helper.IsModified = true;
      }
      // Folder
      else if (e.Data.GetDataPresent(typeof(Folder))) {
        var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;
        var srcData = (Folder)e.Data.GetData(typeof(Folder));
        if (srcData == null) return;

        FoldersViewModel.CopyMove(foMode, srcData, (Folder)destData);
        App.Core.Model.MediaItems.Helper.IsModified = true;
        App.Core.Model.Folders.Helper.IsModified = true;
        App.Core.Model.FolderKeywords.Load();

        // reload last selected source if was moved
        if (foMode == FileOperationMode.Move && srcData.IsSelected) {
          var folder = ((Folder)destData).GetByPath(srcData.Title);
          if (folder == null) return;
          BaseTreeViewItem.ExpandTo(folder);
          App.Core.TreeView_Select(folder, false, false, false);
        }
      }
      // Keyword
      else if (e.Data.GetDataPresent(typeof(Keyword))) {
        var srcData = (Keyword)e.Data.GetData(typeof(Keyword));
        if (srcData == null) return;
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight / 2;
        App.Core.Model.Keywords.ItemMove(srcData, destData, dropOnTop);
      }
      // Person
      else if (e.Data.GetDataPresent(typeof(Person))) {
        var srcData = (Person)e.Data.GetData(typeof(Person));
        if (srcData == null) return;
        App.Core.Model.People.ItemMove(srcData, destData);
      }

      App.Core.Model.Sdb.SaveAllTables();
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private void TreeView_AllowDropCheck(object sender, DragEventArgs e) {
      // scroll treeView when the mouse is near the top or bottom
      var treeView = ((StackPanel)sender).TryFindParent<TreeView>();
      if (treeView != null) {
        var border = VisualTreeHelper.GetChild(treeView, 0);
        if (VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer) {
          var pos = e.GetPosition(treeView);
          if (pos.Y < 25) {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 25);
          }
          else if (treeView.ActualHeight - pos.Y < 25) {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 25);
          }
        }
      }

      // return if the data can be dropped
      var dataContext = ((StackPanel)sender).DataContext;

      // MediaItems
      if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
        var dragged = ((string[])e.Data.GetData(DataFormats.FileDrop))?.OrderBy(x => x).ToArray();
        var selected = App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

        if (dragged != null && selected.SequenceEqual(dragged) &&
            dataContext is Folder destData && destData.IsAccessible) return;
      }
      else // Folder
      if (e.Data.GetDataPresent(typeof(Folder))) {
        var srcData = (Folder)e.Data.GetData(typeof(Folder));

        if (srcData != null && dataContext is Folder destData && !destData.HasThisParent(srcData) &&
            srcData != destData && destData.IsAccessible && (Folder)srcData.Parent != destData) return;
      }
      else // Keyword
      if (e.Data.GetDataPresent(typeof(Keyword))) {
        if (dataContext is BaseTreeViewItem destData &&
            (destData.GetTopParent() as BaseCategoryItem)?.Category == Category.Keywords) return;
      }
      else // Person
      if (e.Data.GetDataPresent(typeof(Person))) {
        var srcData = (Person)e.Data.GetData(typeof(Person));

        if ((dataContext is BaseCategoryItem cat && cat.Category == Category.People) || 
            (dataContext is CategoryGroup group && group.Category == Category.People && srcData?.Parent != group)) return;
      }

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      App.Core.Model.MediaItemSizes.Size.SliderChanged = true;
      App.Core.MediaItemsViewModel.ReapplyFilter();
    }
  }
}
