﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Common.Features.Common;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentsDrawerVM : SegmentCollectionView {
  private readonly SegmentS _segmentS;

  public List<SegmentM> Items { get; }
  public CanDragFunc CanDragFunc { get; }
  public CanDropFunc CanDropFunc { get; }
  public DoDropAction DoDropAction { get; }

  public static AsyncRelayCommand AddSelectedCommand { get; set; } = null!;
  public static RelayCommand OpenCommand { get; set; } = null!;

  public SegmentsDrawerVM(SegmentS segmentS, List<SegmentM> items) {
    _segmentS = segmentS;
    Items = items;

    CanDragFunc = one => _segmentS.GetOneOrSelected(one as SegmentM);
    CanDropFunc = _canDrop;
    DoDropAction = _doDrop;

    AddSelectedCommand = new(
      _ => _addOrRemove(_segmentS.Selected.Items.ToArray(), true),
      () => _segmentS.Selected.Items.Count > 0, Res.IconDrawerAdd, "Add selected to Segments drawer");
    OpenCommand = new(() => _open(Core.VM.MainWindow.ToolsTabs), Res.IconDrawer, "Open Segments drawer");
  }

  private DragDropEffects _canDrop(object? target, object? data, bool haveSameOrigin) {
    if (!haveSameOrigin && !Items.Contains(data))
      return DragDropEffects.Copy;
    if (haveSameOrigin && (data as SegmentM[])?.Contains(target as SegmentM) == false)
      return DragDropEffects.Move;
    return DragDropEffects.None;
  }

  private Task _doDrop(object data, bool haveSameOrigin) =>
    _addOrRemove(data as SegmentM[] ?? (data is SegmentM s ? [s] : []), !haveSameOrigin);

  private async Task _addOrRemove(SegmentM[] segments, bool add) {
    if (!add && await Dialog.ShowAsync(new MessageDialog(
          "Segments Drawer",
          "Do you want to remove segments from drawer?",
          MH.UI.Res.IconQuestion,
          true)) != 1)
      return;

    var count = Items.Count;

    if (add) {
      var toAdd = Items.Concat(segments.Except(Items)).ToArray();
      Items.Clear();

      foreach (var segment in toAdd)
        Items.Add(segment);
    }
    else
      foreach (var segment in segments)
        Items.Remove(segment);

    if (count == Items.Count) return;
    _segmentS.DataAdapter.AreTablePropsModified = true;
    if (add) Insert(segments); else Remove(segments);
  }

  public void RemoveIfContains(SegmentM[] segments) {
    var flag = false;
    foreach (var segment in segments)
      if (Items.Remove(segment))
        flag = true;

    if (!flag) return;
    _segmentS.DataAdapter.AreTablePropsModified = true;
    Remove(segments);
  }

  private void _open(TabControl tt) {
    var source = Items
      .OrderBy(x => x.MediaItem.Folder.FullPath)
      .ThenBy(x => x.MediaItem.FileName)
      .ToList();
    var groupByItems = GroupByItems.GetFolders(source).ToArray();

    Reload(source, GroupMode.GroupByRecursive, groupByItems, true);
    tt.Activate(Res.IconDrawer, "Segments", this);
  }
}