using MH.UI.Controls;
using MH.Utils.EventsArgs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Segment;

public class SegmentCollectionView() : CollectionView<SegmentM>(Res.IconSegment, "Segments", [ViewMode.ThumbSmall]) {
  private static readonly IReadOnlyList<SortField<SegmentM>> _sortFields = [
    new SortField<SegmentM>("File name", x => x.MediaItem.FileName, StringComparer.CurrentCultureIgnoreCase)
  ];

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

  public override int GetItemSize(ViewMode viewMode, SegmentM item, bool getWidth) =>
    SegmentVM.SegmentUiFullWidth;

  public override IEnumerable<SortField<SegmentM>> GetSortFields() => _sortFields;

  public override int SortCompare(SegmentM itemA, SegmentM itemB) =>
    string.Compare(itemA.MediaItem.FileName, itemB.MediaItem.FileName, StringComparison.CurrentCultureIgnoreCase);

  protected override void _onItemSelected(SelectionEventArgs<SegmentM> e) =>
    Core.S.Segment.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

  protected override void _onItemOpened(SegmentM item) =>
    Core.S.Segment.ViewMediaItemsWithSegment(this, item);

  public override string GetItemTemplateName(ViewMode viewMode) => "PM.DT.Segment";
}