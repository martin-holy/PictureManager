using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Utils;

namespace PictureManager.CustomControls {
  public class CatTreeView : TreeView {
    #region Item Commands
    public static RelayCommand<ICatTreeViewItem> ItemCreateCommand { get; } =
      new(item => GetCategory(item)?.ItemCreate(item), item => item != null);

    public static RelayCommand<ICatTreeViewItem> ItemRenameCommand { get; } =
      new(item => GetCategory(item)?.ItemRename(item), item => item != null);

    public static RelayCommand<ICatTreeViewItem> ItemDeleteCommand { get; } =
      new(item => GetCategory(item)?.ItemDelete(item), item => item != null);
    #endregion

    #region Group Commands
    public static RelayCommand<ICatTreeViewCategory> GroupCreateCommand { get; } =
      new(item => GetCategory(item)?.GroupCreate(item), item => item != null);
    
    public static RelayCommand<ICatTreeViewGroup> GroupRenameCommand { get; } =
      new(item => GetCategory(item)?.GroupRename(item), item => item != null);
    
    public static RelayCommand<ICatTreeViewGroup> GroupDeleteCommand { get; } =
      new(item => GetCategory(item)?.GroupDelete(item), item => item != null);
    #endregion

    private ScrollViewer _scrollViewer;
    private double _verticalOffset;

    static CatTreeView() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(CatTreeView), new FrameworkPropertyMetadata(typeof(CatTreeView)));
    }

    private static ICatTreeViewCategory GetCategory(ITreeLeaf item) => Tree.GetTopParent(item) as ICatTreeViewCategory;

    public static void ExpandAll(ICatTreeViewItem root) {
      if (root.Items.Count == 0) return;
      root.IsExpanded = true;
      foreach (var item in root.Items.Cast<ICatTreeViewItem>())
        ExpandAll(item);
    }

    public static void ExpandTo(ICatTreeViewItem item) {
      // expand item as well if it has any sub item and not just placeholder
      if (item.Items.Count > 0 && ((ICatTreeViewItem)item.Items[0]).Parent != null)
        item.IsExpanded = true;
      var parent = (ICatTreeViewItem)item.Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = (ICatTreeViewItem)parent.Parent;
      }
    }

    public void ScrollTo(ICatTreeViewItem item) {
      if (item == null) return;

      var items = new List<ICatTreeViewItem>();
      Tree.GetThisAndParentRecursive(item, ref items);
      items.Reverse();

      var offset = 0.0;
      var parent = this as ItemsControl;
      
      for (var i = 0; i < items.Count; i++) {
        var index = parent.Items.IndexOf(items[i]);
        var panel = parent.FindChild<VirtualizingStackPanel>();
        panel.BringIndexIntoViewPublic(index);
        var tvi = parent.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
        tvi.IsExpanded = true;
        parent = tvi;
        offset += panel.GetItemOffset(tvi);
      }

      _verticalOffset = offset;
      _scrollViewer.ScrollToHorizontalOffset(0);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      ItemTemplateSelector = new CatTreeViewInterfaceTemplateSelector();

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
    private object CanDrag(MouseEventArgs e) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement);
      if (tvi == null || tvi.DataContext is ICatTreeViewCategory) return null;
      if (Tree.GetTopParent(tvi.DataContext as ITreeLeaf) is not ICatTreeViewCategory) return null;
      return tvi.DataContext;
    }

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      DragDropAutoScroll(e);

      var dest = Extensions.FindTemplatedParent<TreeViewItem>(e.OriginalSource as FrameworkElement)?.DataContext;
      var cat = Tree.GetTopParent(dest as ICatTreeViewItem) as ICatTreeViewCategory;

      if (cat?.CanDrop(data, dest as ICatTreeViewItem) == true) {
        if (dest is ICatTreeViewGroup) return DragDropEffects.Move;
        if (!cat.CanCopyItem && !cat.CanMoveItem) return DragDropEffects.None;
        if (cat.CanCopyItem && (e.KeyStates & DragDropKeyStates.ControlKey) != 0) return DragDropEffects.Copy;
        if (cat.CanMoveItem && (e.KeyStates & DragDropKeyStates.ControlKey) == 0) return DragDropEffects.Move;
      }

      return DragDropEffects.None;
    }

    private static void DoDrop(DragEventArgs e, object source, object data) {
      var tvi = Extensions.FindTemplatedParent<TreeViewItem>((FrameworkElement)e.OriginalSource);
      if (tvi?.DataContext is not ICatTreeViewItem dest ||
        Tree.GetTopParent(dest) is not ICatTreeViewCategory cat) return;

      var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
      cat.OnDrop(data, dest, aboveDest, (e.KeyStates & DragDropKeyStates.ControlKey) > 0);
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
    #endregion
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
