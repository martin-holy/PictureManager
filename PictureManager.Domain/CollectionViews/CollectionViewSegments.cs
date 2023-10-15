﻿using MH.UI.Controls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews; 

public class CollectionViewSegments : CollectionView<SegmentM> {
  private readonly SegmentsM _segmentsM;

  public CollectionViewSegments(SegmentsM segmentsM) {
    _segmentsM = segmentsM;
    Icon = Res.IconSegment;
    Name = "Segments";
  }

  public void Reload(List<SegmentM> source, GroupMode groupMode, CollectionViewGroupByItem<SegmentM>[] groupByItems, bool expandAll, string rootTitle, bool removeEmpty = true) {
    Name = rootTitle;
    Reload(source, groupMode, groupByItems, expandAll);
  }

  public override IEnumerable<CollectionViewGroupByItem<SegmentM>> GetGroupByItems(IEnumerable<SegmentM> source) {
    var src = source.ToArray();
    var top = new List<CollectionViewGroupByItem<SegmentM>>();
    top.AddRange(GroupByItems.GetFoldersFromSegments(src));
    top.Add(GroupByItems.GetKeywordsInGroupFromSegments(src));
    top.Add(GroupByItems.GetPeopleInGroupFromSegments(src));

    return top;
  }

  public override int GetItemSize(SegmentM item, bool getWidth) =>
    SegmentsM.SegmentUiFullWidth;

  public override int SortCompare(SegmentM itemA, SegmentM itemB) =>
    string.Compare(itemA.MediaItem.FileName, itemB.MediaItem.FileName, StringComparison.CurrentCultureIgnoreCase);

  public override void OnSelectItem(IEnumerable<SegmentM> source, SegmentM item, bool isCtrlOn, bool isShiftOn) =>
    _segmentsM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

  public override void OnOpenItem(SegmentM item) =>
    _segmentsM.ViewMediaItemsWithSegment(item);
}