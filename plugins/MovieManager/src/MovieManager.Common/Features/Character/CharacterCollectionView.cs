using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Segment;
using System;
using System.Collections.Generic;
using PM = PictureManager.Common;

namespace MovieManager.Common.Features.Character;

public class CharacterCollectionView() : CollectionView<CharacterM>(PM.Res.IconPeople, "Characters") {
  public override IEnumerable<GroupByItem<CharacterM>> GetGroupByItems(IEnumerable<CharacterM> source) => [];

  public override int GetItemSize(ViewMode viewMode, CharacterM item, bool getWidth) =>
    getWidth ? 300 : SegmentVM.SegmentUiFullWidth;

  public override int SortCompare(CharacterM itemA, CharacterM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<CharacterM> e) =>
    Core.S.Character.Select(e);

  public override void OnItemOpened(CharacterM item) {
    if (item.Actor.Image != null)
      Core.VM.PMCoreVM.OpenMediaItems(null, item.Actor.Image);
  }
}