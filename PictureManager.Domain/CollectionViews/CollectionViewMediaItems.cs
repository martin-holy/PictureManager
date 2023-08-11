using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public class CollectionViewMediaItems : CollectionView<MediaItemM> {
    public double ThumbScale { get; set; }
    public Selecting<MediaItemM> Selected { get; } = new();
    public RelayCommand<MouseWheelEventArgs> ZoomCommand { get; }

    public CollectionViewMediaItems(double thumbScale) {
      ThumbScale = thumbScale;
      ZoomCommand = new(e => Zoom(e.Delta), e => e.IsCtrlOn);
    }

    public void Reload(List<MediaItemM> source, GroupMode groupMode, CollectionViewGroupByItem<MediaItemM>[] groupByItems, bool expandAll) {
      SetRoot(new CollectionViewGroup<MediaItemM>(source, Res.IconImageMultiple, "Media Items", this, groupMode, groupByItems), expandAll);
    }

    public override IEnumerable<CollectionViewGroupByItem<MediaItemM>> GetGroupByItems(IEnumerable<MediaItemM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<MediaItemM>>();
      top.Add(GroupByItems.GetDatesInGroupFromMediaItems(src));
      top.Add(GroupByItems.GetPeopleInGroupFromMediaItems(src));
      // TODO remove trunk from folders => remove common branch starting from root
      top.AddRange(GroupByItems.GetFoldersFromMediaItems(src));

      return top;
    }

    public override int GetItemWidth(MediaItemM item) {
      var width = item.ThumbWidth;

      if (Math.Abs(ThumbScale - MediaItemsViews.DefaultThumbScale) > 0)
        width = (int)Math.Round((width / MediaItemsViews.DefaultThumbScale) * ThumbScale, 0);

      return width + 6;
    }

    public override int SortCompare(MediaItemM itemA, MediaItemM itemB) =>
      string.Compare(itemA.FileName, itemB.FileName, StringComparison.CurrentCultureIgnoreCase);

    public override void OnSelectItem(IEnumerable<MediaItemM> source, MediaItemM item, bool isCtrlOn, bool isShiftOn) =>
      Selected.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    private void Zoom(int delta) {
      if (delta < 0 && ThumbScale < .1) return;
      ThumbScale += delta > 0 ? .05 : -.05;
      ReWrapAll();
    }
  }
}
