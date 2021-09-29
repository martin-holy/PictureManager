using MahApps.Metro.Controls;
using PictureManager.Commands;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PictureManager.CustomControls {
  public class CatTreeView : TreeView {
    private ScrollViewer _scrollViewer;

    static CatTreeView() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(CatTreeView), new FrameworkPropertyMetadata(typeof(CatTreeView)));
    }

    public void ScrollTo(ICatTreeViewItem item) {
      UpdateLayout();
      var items = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndParentRecursive(item, ref items);
      items.Reverse();
      var tvi = ItemContainerGenerator.ContainerFromItem(items[0]) as TreeViewItem;

      for (var i = 1; i < items.Count; i++) {
        if (tvi == null) break;
        tvi = tvi.ItemContainerGenerator.ContainerFromItem(items[i]) as TreeViewItem;
      }

      var sv = this.FindChildren<ScrollViewer>(true).SingleOrDefault();
      sv?.ScrollToBottom();
      tvi?.BringIntoView();
      sv?.ScrollToHorizontalOffset(0);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      ItemTemplateSelector = new CatTreeViewInterfaceTemplateSelector();

      _scrollViewer = Template.FindName("CatTreeViewScrollViewer", this) as ScrollViewer;

      PreviewMouseRightButtonDown += AttachContextMenu;

      DragDropFactory.SetDrag(this, CanDrag);
      DragDropFactory.SetDrop(this, CanDrop, DoDrop);
    }

    private object CanDrag(MouseEventArgs e) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement);
      if (tvi == null || tvi.DataContext is ICatTreeViewCategory) return null;
      if (CatTreeViewUtils.GetTopParent(tvi.DataContext as ICatTreeViewItem) is not ICatTreeViewCategory) return null;
      return tvi.DataContext;
    }

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      DragDropAutoScroll(e);

      var dest = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement)?.DataContext;
      var cat = CatTreeViewUtils.GetTopParent(dest as ICatTreeViewItem) as ICatTreeViewCategory;

      if (cat?.CanDrop(data, dest as ICatTreeViewItem) == true) {
        if (dest is ICatTreeViewGroup) return DragDropEffects.Move;
        if (!cat.CanCopyItem && !cat.CanMoveItem) return DragDropEffects.None;
        if (cat.CanCopyItem && (e.KeyStates & DragDropKeyStates.ControlKey) != 0) return DragDropEffects.Copy;
        if (cat.CanMoveItem && (e.KeyStates & DragDropKeyStates.ControlKey) == 0) return DragDropEffects.Move;
      }

      return DragDropEffects.None;
    }

    private void DoDrop(DragEventArgs e, object source, object data) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);
      if (tvi?.DataContext is not ICatTreeViewItem dest ||
        CatTreeViewUtils.GetTopParent(dest) is not ICatTreeViewCategory cat) return;

      var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
      cat.OnDrop(data, dest, aboveDest, (e.KeyStates & DragDropKeyStates.ControlKey) > 0);

      // TODO send args in EventArgs
      CatTreeViewUtils.OnAfterOnDrop?.Invoke(
        new[] { data, dest, aboveDest, (e.KeyStates & DragDropKeyStates.ControlKey) > 0 },
        EventArgs.Empty);
    }

    /// <summary>
    /// Scroll treeView when the mouse is near the top or bottom
    /// </summary>
    private void DragDropAutoScroll(DragEventArgs e) {
      var pos = e.GetPosition(this);
      if (pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - 25);
      else if (ActualHeight - pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + 25);
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //e.Handled = true;

      var b = Extensions.FindThisOrParent<Border>(e.OriginalSource as FrameworkElement, "Border");

      if (b == null || b.ContextMenu != null || b.DataContext is not ICatTreeViewItem item) return;

      var menu = new ContextMenu();
      var binding = new Binding(nameof(ContextMenu.PlacementTarget)) {
        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1)
      };

      void AddMenuItem(ICommand command) {
        var menuItem = new MenuItem { Command = command, CommandParameter = item };
        menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);
        menu.Items.Add(menuItem);
      }

      if (CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory category) {
        if (category.CanCreateItems || category.CanRenameItems || category.CanDeleteItems) {
          var cat = b.DataContext as ICatTreeViewCategory;
          var group = b.DataContext as ICatTreeViewGroup;

          if (category.CanCreateItem(item) && (cat != null || group != null || category.CanHaveSubItems))
            AddMenuItem(CatTreeViewCommands.ItemNewCommand);

          if (cat == null && group == null) {
            if (category.CanRenameItem(item))
              AddMenuItem(CatTreeViewCommands.ItemRenameCommand);
            if (category.CanDeleteItem(item))
              AddMenuItem(CatTreeViewCommands.ItemDeleteCommand);
          }

          if (category.CanHaveGroups && cat != null)
            AddMenuItem(CatTreeViewCommands.GroupNewCommand);

          if (group != null) {
            AddMenuItem(CatTreeViewCommands.GroupRenameCommand);
            AddMenuItem(CatTreeViewCommands.GroupDeleteCommand);
          }

          if (category.CanSort(item))
            AddMenuItem(CatTreeViewCommands.SortCommand);
        }
      }

      if (menu.Items.Count > 0)
        b.ContextMenu = menu;
    }
  }

  public sealed class CatTreeViewInterfaceTemplateSelector : DataTemplateSelector {
    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      if (item == null || container is not FrameworkElement containerElement)
        return base.SelectTemplate(item, container);

      var itemType = item.GetType();
      var dataTypes = new[] {
        itemType,
        itemType.GetInterface(nameof(ICatTreeViewCategory)),
        itemType.GetInterface(nameof(ICatTreeViewItem))
      };

      var template = dataTypes.Where(t => t != null)
        .Select(t => new DataTemplateKey(t))
        .Select(containerElement.TryFindResource)
        .OfType<HierarchicalDataTemplate>().FirstOrDefault();

      return template ?? base.SelectTemplate(item, container);
    }
  }
}
