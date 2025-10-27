using MH.UI.Controls;
using MH.Utils;
using MH.Utils.EventsArgs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Person;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem.Video;

public class VideoItemCollectionView : CollectionView<VideoItemM> {
  private static readonly IReadOnlyList<SortField<VideoItemM>> _sortFields = [
    new SortField<VideoItemM>("Time", x => x.TimeStart, StringComparer.CurrentCultureIgnoreCase)
  ];

  public Selecting<VideoItemM> Selected { get; } = new();

  public VideoItemCollectionView() : base(Res.IconImageMultiple, "Video Items", [ViewMode.ThumbBig]) {
    DefaultSortField = _sortFields[0];
  }

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

  public override IEnumerable<SortField<VideoItemM>> GetSortFields() => _sortFields;

  public override int SortCompare(VideoItemM itemA, VideoItemM itemB) =>
    itemA.TimeStart - itemB.TimeStart;

  protected override void _onItemSelected(SelectionEventArgs<VideoItemM> e) =>
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

  public override string GetItemTemplateName(ViewMode viewMode) => "PM.DT.VideoItem.Thumb";
}