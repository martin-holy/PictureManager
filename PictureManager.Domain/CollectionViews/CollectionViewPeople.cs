using MH.UI.Controls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews;

public class CollectionViewPeople : CollectionView<PersonM> {
  public CollectionViewPeople() {
    Icon = Res.IconPeopleMultiple;
    Name = "People";
  }

  public override IEnumerable<GroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<PersonM>>();
    top.Add(GroupByItems.GetPeopleGroupsInGroupFromPeople(src));
    top.Add(GroupByItems.GetKeywordsInGroupFromPeople(src));

    return top;
  }

  public override int GetItemSize(PersonM item, bool getWidth) =>
    SegmentsM.SegmentUiFullWidth;

  public override int SortCompare(PersonM itemA, PersonM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);
}