using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem.Video;

public class VideoItemCollectionView() : CollectionView<VideoItemM>(Res.IconImageMultiple, "Video Items", [ViewMode.ThumbBig]) {
  public Selecting<VideoItemM> Selected { get; } = new();

  public override int GetItemSize(ViewMode viewMode, VideoItemM item, bool getWidth) =>
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

  public override string GetItemTemplateName(ViewMode viewMode) => "PM.DT.VideoItem.Thumb";
}