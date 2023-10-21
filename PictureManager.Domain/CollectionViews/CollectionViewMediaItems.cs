using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
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

  public override int GetItemSize(MediaItemM item, bool getWidth) {
    var size = getWidth ? item.ThumbWidth : item.ThumbHeight;

    if (Math.Abs(ThumbScale - MediaItemsViews.DefaultThumbScale) > 0)
      size = (int)Math.Round((size / MediaItemsViews.DefaultThumbScale) * ThumbScale, 0);

    return size;
  }

  public override int SortCompare(MediaItemM itemA, MediaItemM itemB) =>
    string.Compare(itemA.FileName, itemB.FileName, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<MediaItemM> e) =>
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
}