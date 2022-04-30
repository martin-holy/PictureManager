using MH.UI.WPF.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MH.UI.WPF.Controls {
  public class CatTreeView : TreeView {
    public static readonly DependencyProperty ScrollToItemProperty = DependencyProperty.Register(
      nameof(ScrollToItem),
      typeof(ITreeItem),
      typeof(CatTreeView),
      new(ScrollToItemChanged));

    public ITreeItem ScrollToItem {
      get => (ITreeItem)GetValue(ScrollToItemProperty);
      set => SetValue(ScrollToItemProperty, value);
    }

    public static RelayCommand<ITreeItem> ItemCreateCommand { get; } = new(
      item => GetCategory(item)?.ItemCreate(item),
      item => item != null);

    public static RelayCommand<ITreeItem> ItemRenameCommand { get; } = new(
      item => GetCategory(item)?.ItemRename(item),
      item => item != null);

    public static RelayCommand<ITreeItem> ItemDeleteCommand { get; } = new(
      item => GetCategory(item)?.ItemDelete(item),
      item => item != null);

    public static RelayCommand<ITreeCategory> GroupCreateCommand { get; } = new(
      item => GetCategory(item)?.GroupCreate(item),
      item => item != null);

    public static RelayCommand<ITreeGroup> GroupRenameCommand { get; } = new(
      item => GetCategory(item)?.GroupRename(item),
      item => item != null);

    public static RelayCommand<ITreeGroup> GroupDeleteCommand { get; } = new(
      item => GetCategory(item)?.GroupDelete(item),
      item => item != null);

    private ScrollViewer _scrollViewer;
    private double _verticalOffset;

    static CatTreeView() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(CatTreeView), new FrameworkPropertyMetadata(typeof(CatTreeView)));
    }

    private static void ScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as CatTreeView)?.ScrollTo((ITreeItem)e.NewValue);

    private static ITreeCategory GetCategory(ITreeItem item) =>
      Tree.GetTopParent(item) as ITreeCategory;

    private void ScrollTo(ITreeItem item) {
      if (item == null) return;

      var items = new List<ITreeItem>();
      Tree.GetThisAndParentRecursive(item, ref items);
      items.Reverse();

      var offset = 0.0;
      var parent = this as ItemsControl;

      foreach (var treeItem in items) {
        var index = parent.Items.IndexOf(treeItem);
        var panel = parent.GetChildOfType<VirtualizingStackPanel>();
        if (panel == null) break;
        panel.BringIndexIntoViewPublic(index);
        if (parent.ItemContainerGenerator.ContainerFromIndex(index) is not TreeViewItem tvi) break;
        tvi.IsExpanded = true;
        parent = tvi;
        offset += panel.GetItemOffset(tvi);
      }

      _verticalOffset = offset;
      _scrollViewer?.ScrollToHorizontalOffset(0);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      _scrollViewer = Template.FindName("PART_ScrollViewer", this) as ScrollViewer;

      LayoutUpdated += (_, _) => {
        if (_verticalOffset > 0) {
          _scrollViewer.ScrollToVerticalOffset(_verticalOffset);
          _verticalOffset = 0;
        }
      };

      DragDropFactory.SetDrag(this, CanDrag);
      DragDropFactory.SetDrop(this, CanDrop, DoDrop);
    }

    #region Drag & Drop
    private static object CanDrag(MouseEventArgs e) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement);
      return tvi == null || tvi.DataContext is ITreeCategory
        ? null
        : Tree.GetTopParent(tvi.DataContext as ITreeItem) is not ITreeCategory
          ? null
          : tvi.DataContext;
    }

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      DragDropAutoScroll(e);

      var dest = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement)?.DataContext;
      var cat = Tree.GetTopParent(dest as ITreeItem) as ITreeCategory;

      if (cat?.CanDrop(data, dest as ITreeItem) == true) {
        if (dest is ITreeGroup) return DragDropEffects.Move;
        if (!cat.CanCopyItem && !cat.CanMoveItem) return DragDropEffects.None;
        if (cat.CanCopyItem && (e.KeyStates & DragDropKeyStates.ControlKey) != 0) return DragDropEffects.Copy;
        if (cat.CanMoveItem && (e.KeyStates & DragDropKeyStates.ControlKey) == 0) return DragDropEffects.Move;
      }

      return DragDropEffects.None;
    }

    private static void DoDrop(DragEventArgs e, object source, object data) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);
      if (tvi?.DataContext is not ITreeItem dest ||
        Tree.GetTopParent(dest) is not ITreeCategory cat) return;

      var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
      cat.OnDrop(data, dest, aboveDest, (e.KeyStates & DragDropKeyStates.ControlKey) > 0);
    }

    /// <summary>
    /// Scroll TreeView when the mouse is near the top or bottom
    /// </summary>
    private void DragDropAutoScroll(DragEventArgs e) {
      var pos = e.GetPosition(this);
      if (pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - 25);
      else if (ActualHeight - pos.Y < 25)
        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + 25);
    }
    #endregion
  }
}
