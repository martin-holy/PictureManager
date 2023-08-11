using MH.UI.Controls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public class CollectionViewSegments : CollectionView<SegmentM> {
    private readonly SegmentsM _segmentsM;

    public CollectionViewSegments(SegmentsM segmentsM) {
      _segmentsM = segmentsM;
    }

    public void Reload(List<SegmentM> source, GroupMode groupMode, CollectionViewGroupByItem<SegmentM>[] groupByItems, bool expandAll, string rootTitle = "Segments", bool removeEmpty = true) {
      SetRoot(new CollectionViewGroup<SegmentM>(source, Res.IconSegment, rootTitle, this, groupMode, groupByItems), expandAll, removeEmpty);
    }

    public override IEnumerable<CollectionViewGroupByItem<SegmentM>> GetGroupByItems(IEnumerable<SegmentM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<SegmentM>>();
      top.AddRange(GroupByItems.GetFoldersFromSegments(src));
      top.Add(GroupByItems.GetKeywordsInGroupFromSegments(src));
      top.Add(GroupByItems.GetPeopleInGroupFromSegments(src));

      return top;
    }

    public override int GetItemWidth(SegmentM item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override int SortCompare(SegmentM itemA, SegmentM itemB) =>
      string.Compare(itemA.MediaItem.FileName, itemB.MediaItem.FileName, StringComparison.CurrentCultureIgnoreCase);

    public override void OnSelectItem(IEnumerable<SegmentM> source, SegmentM item, bool isCtrlOn, bool isShiftOn) =>
      _segmentsM.Select(source.ToList(), item, isCtrlOn, isShiftOn);
  }
}
