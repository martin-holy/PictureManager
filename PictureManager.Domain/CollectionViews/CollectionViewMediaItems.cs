using MH.UI.Controls;
using MH.Utils;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews;

public class CollectionViewMediaItems : CollectionView<MediaItemM> {
  private double _thumbScale;

  public double ThumbScale {
    get => _thumbScale;
    set {
      _thumbScale = value;
      if (Root != null) ReWrapAll();
      OnPropertyChanged();
    }
  }

  public Selecting<MediaItemM> Selected { get; } = new();

  public CollectionViewMediaItems(double thumbScale) {
    Icon = Res.IconImageMultiple;
    Name = "Media Items";
    ThumbScale = thumbScale;
  }

  public override IEnumerable<CollectionViewGroupByItem<MediaItemM>> GetGroupByItems(IEnumerable<MediaItemM> source) {
    var src = source.ToArray();
    var top = new List<CollectionViewGroupByItem<MediaItemM>>();
    // TODO remove trunk from folders => remove common branch starting from root
    top.AddRange(GroupByItems.GetFoldersFromMediaItems(src));
    top.Add(GroupByItems.GetDatesInGroupFromMediaItems(src));
    top.Add(GroupByItems.GetKeywordsInGroupFromMediaItems(src));
    top.Add(GroupByItems.GetPeopleInGroupFromMediaItems(src));

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
}