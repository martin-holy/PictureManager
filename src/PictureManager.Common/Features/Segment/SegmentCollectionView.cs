using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Segment;

public class SegmentCollectionView() : CollectionView<SegmentM>(Res.IconSegment, "Segments") {
  public void Reload(List<SegmentM> source, GroupMode groupMode, GroupByItem<SegmentM>[]? groupByItems, bool expandAll, string rootTitle, bool removeEmpty = true) {
    Name = rootTitle;
    Reload(source, groupMode, groupByItems, expandAll, removeEmpty);
  }

  public override IEnumerable<GroupByItem<SegmentM>> GetGroupByItems(IEnumerable<SegmentM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<SegmentM>>();
    top.AddRange(GroupByItems.GetFolders(src));
    top.AddRange(GroupByItems.GetGeoNames(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.Add(GroupByItems.GetPeopleInGroup(src));
    top.Add(GroupByItems.GetSegmentSizesInGroup(src));

    return top;
  }

  public override int GetItemSize(SegmentM item, bool getWidth) =>
    Core.VM.Segment.SegmentUiFullWidth;

  public override int SortCompare(SegmentM itemA, SegmentM itemB) =>
    string.Compare(itemA.MediaItem.FileName, itemB.MediaItem.FileName, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<SegmentM> e) =>
    Core.S.Segment.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

  public override void OnItemOpened(SegmentM item) =>
    Core.S.Segment.ViewMediaItemsWithSegment(this, item);
}