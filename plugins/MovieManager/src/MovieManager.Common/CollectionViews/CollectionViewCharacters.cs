﻿using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using System;
using System.Collections.Generic;

namespace MovieManager.Common.CollectionViews;

public class CollectionViewCharacters : CollectionView<CharacterM> {
  public CollectionViewCharacters() {
    Icon = "IconBug";
    Name = "Characters";
  }

  public override IEnumerable<GroupByItem<CharacterM>> GetGroupByItems(IEnumerable<CharacterM> source) => [];

  public override int GetItemSize(CharacterM item, bool getWidth) =>
    getWidth ? 200 : Core.VM.PhCoreVM.Segment.SegmentUiFullWidth;

  public override int SortCompare(CharacterM itemA, CharacterM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<CharacterM> e) =>
    Core.S.Character.Select(e);
}