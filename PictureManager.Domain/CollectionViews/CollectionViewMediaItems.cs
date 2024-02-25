﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models.MediaItems;
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

  public override IEnumerable<GroupByItem<MediaItemM>> GetGroupByItems(IEnumerable<MediaItemM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<MediaItemM>>();
    // TODO remove trunk from folders => remove common branch starting from root
    top.AddRange(GroupByItems.GetFolders(src));
    top.Add(GroupByItems.GetDatesInGroup(src));
    top.AddRange(GroupByItems.GetGeoNames(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.Add(GroupByItems.GetPeopleInGroup(src));

    return top;
  }

  public override int GetItemSize(MediaItemM item, bool getWidth) =>
    (int)((getWidth ? item.ThumbWidth : item.ThumbHeight) * ThumbScale);

  public override int SortCompare(MediaItemM itemA, MediaItemM itemB) =>
    string.Compare(itemA.FileName, itemB.FileName, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<MediaItemM> e) =>
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
}