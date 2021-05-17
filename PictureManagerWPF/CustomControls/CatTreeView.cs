using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PictureManager.Commands;
using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.CustomControls {
  public class CatTreeView: TreeView {
    private ScrollViewer _scrollViewer;
    
    static CatTreeView() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(CatTreeView),
        new FrameworkPropertyMetadata(typeof(CatTreeView)));
    }

    public void ScrollTo(ICatTreeViewBaseItem item) {
      var items = new List<ICatTreeViewBaseItem>();
      CatTreeViewUtils.GetThisAndParentRecursive(item, ref items);
      items.Reverse();
      var tvi = ItemContainerGenerator.ContainerFromItem(items[0]) as TreeViewItem;

      for (var i = 1; i < items.Count; i++) {
        if (tvi == null) break;
        tvi = tvi.ItemContainerGenerator.ContainerFromItem(items[i]) as TreeViewItem;
      }

      tvi?.Focus();
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      ItemTemplateSelector = new CatTreeViewInterfaceTemplateSelector();

      _scrollViewer = Template.FindName("CatTreeViewScrollViewer", this) as ScrollViewer;

      PreviewMouseRightButtonDown += AttachContextMenu;
      MouseMove += Scroll;

      // Drag & Drop
      PreviewMouseLeftButtonDown += SetDragObject;
      MouseMove += StartDragDrop;
      DragEnter += AllowDropCheck;
      DragLeave += AllowDropCheck;
      DragOver += AllowDropCheck;
      Drop += OnDrop;
    }

    private void Scroll(object sender, MouseEventArgs e) {
      // scroll treeView when the mouse is near the top or bottom
      if (e.LeftButton != MouseButtonState.Pressed) return;

      var pos = e.GetPosition(this);
      if (pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - 25);
      else if (ActualHeight - pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + 25);
    }

    #region  Drag & Drop

    private Point _dragDropStartPosition;
    private FrameworkElement _dragDropSource;

    private void SetDragObject(object sender, MouseButtonEventArgs e) {
      _dragDropSource = null;
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);
      if (tvi == null || tvi.DataContext is ICatTreeViewCategory) return;
      _dragDropSource = tvi;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void StartDragDrop(object sender, MouseEventArgs e) {
      if (_dragDropSource == null || !IsDragDropStarted(e)) return;
      DragDrop.DoDragDrop(_dragDropSource, new[] { _dragDropSource.DataContext }, DragDropEffects.Move);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private static void AllowDropCheck(object sender, DragEventArgs e) {
      // return if the data can be dropped
      var dest = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource)?.DataContext;
      var src = ((object[]) e.Data.GetData(typeof(object[])))?[0];

      if (dest != src) {
        var destCat = CatTreeViewUtils.GetTopParent(dest as ICatTreeViewBaseItem) as ICatTreeViewCategory;
        var srcCat = CatTreeViewUtils.GetTopParent(src as ICatTreeViewBaseItem) as ICatTreeViewCategory;

        // copy/move within same category
        if (destCat != null && srcCat != null && destCat.Category == srcCat.Category) {
          // copy/move groups
          if (dest is ICatTreeViewGroup && src is ICatTreeViewGroup)
            return;

          // copy/move items
          if (!(src is ICatTreeViewGroup) && src is ICatTreeViewBaseItem srcItem && 
              dest != srcItem.Parent && dest is ICatTreeViewBaseItem)
            return;
        }
      }

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement) e.OriginalSource);
      if (tvi == null) return;

      var cat = CatTreeViewUtils.GetTopParent(tvi.DataContext as ICatTreeViewBaseItem) as ICatTreeViewCategory;
      if (cat == null) return;

      var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
      var dest = tvi.DataContext;
      var src = ((object[]) e.Data.GetData(typeof(object[])))?[0];
      

      // copy/move groups
      if (src is ICatTreeViewGroup srcGroup && dest is ICatTreeViewGroup destGroup) {
        cat.GroupMove(srcGroup, destGroup, aboveDest);
        return;
      }

      // copy/move items
      if (src is ICatTreeViewBaseItem srcItem && dest is ICatTreeViewBaseItem destItem)
        cat.ItemMove(srcItem, destItem, aboveDest);
    }

    #endregion

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      e.Handled = true;

      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);

      if (tvi == null || tvi.ContextMenu != null) return;

      var menu = new ContextMenu { Tag = tvi.DataContext };
      var binding = new Binding(nameof(ContextMenu.PlacementTarget)) {
        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1)
      };

      void AddMenuItem(ICommand command) {
        var menuItem = new MenuItem { Command = command, CommandParameter = tvi.DataContext };
        menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);
        menu.Items.Add(menuItem);
      }

      if (CatTreeViewUtils.GetTopParent(tvi.DataContext as ICatTreeViewBaseItem) is ICatTreeViewCategory category) {
        if (category.CanModifyItems) {
          var cat = tvi.DataContext as ICatTreeViewCategory;
          var group = tvi.DataContext as ICatTreeViewGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            AddMenuItem(CatTreeViewCommands.ItemNewCommand);
          }

          if (tvi.DataContext is ICatTreeViewBaseItem && cat == null && group == null) {
            AddMenuItem(CatTreeViewCommands.ItemRenameCommand);
            AddMenuItem(CatTreeViewCommands.ItemDeleteCommand);
          }

          if (category.CanHaveGroups && cat != null)
            AddMenuItem(CatTreeViewCommands.GroupNewCommand);

          if (group != null) {
            AddMenuItem(CatTreeViewCommands.GroupRenameCommand);
            AddMenuItem(CatTreeViewCommands.GroupDeleteCommand);
          }
        }
      }

      if (menu.Items.Count > 0)
        tvi.ContextMenu = menu;
    }
  }

  public sealed class CatTreeViewInterfaceTemplateSelector : DataTemplateSelector {
    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      if (item == null || !(container is FrameworkElement containerElement))
        return base.SelectTemplate(item, container);

      var itemType = item.GetType();
      var dataTypes = new[] {
        itemType, 
        itemType.GetInterface(nameof(ICatTreeViewCategory)),
        itemType.GetInterface(nameof(ICatTreeViewBaseItem))
      };

      var template = dataTypes.Where(t => t != null)
        .Select(t => new DataTemplateKey(t))
        .Select(containerElement.TryFindResource)
        .OfType<HierarchicalDataTemplate>().FirstOrDefault();

      return template ?? base.SelectTemplate(item, container);
    }
  }
}
