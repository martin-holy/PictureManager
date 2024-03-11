using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.CollectionViews;

public class CollectionViewPeople : CollectionView<PersonM> {
  public CollectionViewPeople() {
    Icon = Res.IconPeopleMultiple;
    Name = "People";
  }

  public override IEnumerable<GroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<PersonM>>();
    top.Add(GroupByItems.GetPeopleGroupsInGroup(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));

    return top;
  }

  public override int GetItemSize(PersonM item, bool getWidth) =>
    SegmentS.SegmentUiFullWidth;

  public override int SortCompare(PersonM itemA, PersonM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<PersonM> e) =>
    Core.S.Person.Select(e);

  public override void OnItemOpened(PersonM item) =>
    Core.S.Segment.ViewMediaItemsWithSegment(this, item.Segment);
}