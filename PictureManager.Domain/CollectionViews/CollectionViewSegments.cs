using MH.UI.Controls;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public class CollectionViewSegments : CollectionView<SegmentM> {
    private readonly SegmentsM _segmentsM;

    public CollectionViewSegments(SegmentsM segmentsM) {
      _segmentsM = segmentsM;
    }

    public void Reload(List<SegmentM> source, GroupMode groupMode, CollectionViewGroupByItem<SegmentM>[] groupByItems, bool expandAll) {
      SetRoot(new(source, Res.IconSegment, "Segments", this, groupMode, groupByItems), expandAll);
    }

    public override IEnumerable<CollectionViewGroupByItem<SegmentM>> GetGroupByItems(IEnumerable<SegmentM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<SegmentM>>();
      top.AddRange(GroupByItems.GetFoldersFromSegments(src));
      top.Add(GroupByItems.GetKeywordsInGroupFromSegments(src));
      top.Add(GroupByItems.GetPeopleInGroupFromSegments(src));

      return top;
    }

    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override string ItemOrderBy(SegmentM item) =>
      item.MediaItem.FileName;

    public override void Select(IEnumerable<SegmentM> source, SegmentM item, bool isCtrlOn, bool isShiftOn) =>
      _segmentsM.Select(source.ToList(), item, isCtrlOn, isShiftOn);
  }
}
