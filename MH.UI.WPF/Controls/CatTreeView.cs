﻿using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Interfaces;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.Extensions;
using static MH.Utils.DragDropHelper;

namespace MH.UI.WPF.Controls;

public class CatTreeView : TreeViewBase {
  public CanDragFunc CanDragFunc { get; }
  public CanDropFunc CanDropFunc { get; }
  public DoDropAction DoDropAction { get; }

  static CatTreeView() {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(CatTreeView), new FrameworkPropertyMetadata(typeof(CatTreeView)));
  }

  public CatTreeView() {
    CanDragFunc = CanDrag;
    CanDropFunc = CanDrop;
    DoDropAction = DoDrop;
  }

  private static object CanDrag(object source) {
    return source is ITreeCategory
      ? null
      : Tree.GetParentOf<ITreeCategory>(source as ITreeItem) is null
        ? null
        : source;
  }

  private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
    var e = Utils.DragDropHelper.DragEventArgs;
    DragDropAutoScroll(e);

    var cat = Tree.GetParentOf<ITreeCategory>(target as ITreeItem);

    if (cat?.CanDrop(data, target as ITreeItem) == true) {
      if (target is ITreeGroup) return MH.Utils.DragDropEffects.Move;
      if (!cat.CanCopyItem && !cat.CanMoveItem) return MH.Utils.DragDropEffects.None;
      if (cat.CanCopyItem && (e.KeyStates & DragDropKeyStates.ControlKey) != 0) return MH.Utils.DragDropEffects.Copy;
      if (cat.CanMoveItem && (e.KeyStates & DragDropKeyStates.ControlKey) == 0) return MH.Utils.DragDropEffects.Move;
    }

    return MH.Utils.DragDropEffects.None;
  }

  private static void DoDrop(object data, bool haveSameOrigin) {
    var e = Utils.DragDropHelper.DragEventArgs;
    var tvi = ((FrameworkElement)e.OriginalSource).FindTemplatedParent<TreeViewItem>();
    if (tvi?.DataContext is not ITreeItem dest ||
        Tree.GetParentOf<ITreeCategory>(dest) is not { } cat) return;

    var aboveDest = e.GetPosition(tvi).Y < tvi.ActualHeight / 2;
    cat.OnDrop(data, dest, aboveDest, (e.KeyStates & DragDropKeyStates.ControlKey) > 0);
  }
}