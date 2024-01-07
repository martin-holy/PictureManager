﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews;

public class CollectionViewVideoItems : CollectionView<VideoItemM> {
  public Selecting<VideoItemM> Selected { get; } = new();

  public override int GetItemSize(VideoItemM item, bool getWidth) =>
    (getWidth ? item.ThumbWidth : item.ThumbHeight) / 3;

  public override IEnumerable<GroupByItem<VideoItemM>> GetGroupByItems(IEnumerable<VideoItemM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<VideoItemM>>();
    top.AddRange(GroupByItems.GetFolders(src));
    top.Add(GroupByItems.GetDatesInGroup(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.Add(GroupByItems.GetPeopleInGroup(src));

    return top;
  }

  public override int SortCompare(VideoItemM itemA, VideoItemM itemB) =>
    itemA.TimeStart - itemB.TimeStart;

  public override void OnItemSelected(SelectionEventArgs<VideoItemM> e) {
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
    Core.VideoDetail.MediaPlayer.SetCurrent(Selected.Items.FirstOrDefault());
  }
}