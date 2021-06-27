using MahApps.Metro.Controls;
using PictureManager.Commands;
using PictureManager.Domain.CatTreeViewModels;
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

      // Drag & Drop
      PreviewMouseLeftButtonDown += SetDragObject;
      PreviewMouseLeftButtonUp += ReleaseDragObject;
      MouseMove += StartDragDrop;
      DragEnter += AllowDropCheck;
      DragLeave += AllowDropCheck;
      DragOver += AllowDropCheck;
      Drop += OnDrop;
    }

    #region Drag & Drop

    private Point _dragDropStartPosition;
    private FrameworkElement _dragDropSource;
    private DragDropEffects _dragDropEffects;

    private void SetDragObject(object sender, MouseButtonEventArgs e) {
      _dragDropSource = null;
      _dragDropStartPosition = new Point(0, 0);
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);
      if (tvi == null || tvi.DataContext is ICatTreeViewCategory) return;
      if (CatTreeViewUtils.GetTopParent(tvi.DataContext as ICatTreeViewItem) is not ICatTreeViewCategory cat) return;

      if (tvi.DataContext is ICatTreeViewGroup)
        _dragDropEffects = DragDropEffects.Move;
      else {
        if (!cat.CanCopyItem && !cat.CanMoveItem) return;

        if (cat.CanCopyItem && cat.CanMoveItem)
          _dragDropEffects = DragDropEffects.Copy | DragDropEffects.Move;
        else if (cat.CanCopyItem)
          _dragDropEffects = DragDropEffects.Copy;
        else if (cat.CanMoveItem)
          _dragDropEffects = DragDropEffects.Move;
      }

      _dragDropSource = tvi;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void ReleaseDragObject(object sender, MouseButtonEventArgs e) => _dragDropSource = null;

    private void StartDragDrop(object sender, MouseEventArgs e) {
      if (_dragDropSource == null || !IsDragDropStarted(e)) return;
      DragDrop.DoDragDrop(_dragDropSource, new[] { _dragDropSource.DataContext }, _dragDropEffects);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void AllowDropCheck(object sender, DragEventArgs e) {
      // scroll treeView when the mouse is near the top or bottom
      var pos = e.GetPosition(this);
      if (pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - 25);
      else if (ActualHeight - pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + 25);

      var dest = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource)?.DataContext;
      var destCat = CatTreeViewUtils.GetTopParent(dest as ICatTreeViewItem) as ICatTreeViewCategory;
      var src = ((object[])e.Data.GetData(typeof(object[])))?[0] ??
                (string[])e.Data.GetData(DataFormats.FileDrop);

      if (destCat?.CanDrop(src, dest as ICatTreeViewItem) == true) return;

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);

      if (tvi?.DataContext is not ICatTreeViewItem dest ||
        CatTreeViewUtils.GetTopParent(dest) is not ICatTreeViewCategory cat) return;

      var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
      var src = ((object[])e.Data.GetData(typeof(object[])))?[0] ??
                (string[])e.Data.GetData(DataFormats.FileDrop);

      cat.OnDrop(src, dest, aboveDest, e.KeyStates == DragDropKeyStates.ControlKey);

      // TODO send args in EventArgs
      CatTreeViewUtils.OnAfterOnDrop?.Invoke(
        new[] { src, dest, aboveDest, e.KeyStates == DragDropKeyStates.ControlKey },
        EventArgs.Empty);
    }

    #endregion

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
