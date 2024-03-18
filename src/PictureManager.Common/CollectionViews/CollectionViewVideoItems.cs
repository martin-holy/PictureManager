using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.CollectionViews;

public class CollectionViewVideoItems : CollectionView<VideoItemM> {
  public Selecting<VideoItemM> Selected { get; } = new();

  public CollectionViewVideoItems() {
    Icon = Res.IconImageMultiple;
    Name = "Video Items";
  }

  public override int GetItemSize(VideoItemM item, bool getWidth) =>
    (int)((getWidth ? item.ThumbWidth : item.ThumbHeight) * Core.Settings.MediaItem.VideoItemThumbScale);

  public override IEnumerable<GroupByItem<VideoItemM>> GetGroupByItems(IEnumerable<VideoItemM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<VideoItemM>>();
    top.AddRange(GroupByItems.GetFolders(src));
    top.Add(GroupByItems.GetDatesInGroup(src));
    top.AddRange(GroupByItems.GetGeoNames(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.Add(GroupByItems.GetPeopleInGroup(src));

    return top;
  }

  public override int SortCompare(VideoItemM itemA, VideoItemM itemB) =>
    itemA.TimeStart - itemB.TimeStart;

  public override void OnItemSelected(SelectionEventArgs<VideoItemM> e) =>
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
}