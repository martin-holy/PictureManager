using MH.UI.Controls;
using MH.Utils.BaseClasses;
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
    top.Add(GroupByItems.GetPeopleGroupsInGroup(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));

    return top;
  }

  public override int GetItemSize(PersonM item, bool getWidth) =>
    SegmentsM.SegmentUiFullWidth;

  public override int SortCompare(PersonM itemA, PersonM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<PersonM> e) =>
    Core.PeopleM.Select(e);

  public override void OnItemOpened(PersonM item) =>
    Core.M.Segments.ViewMediaItemsWithSegment(this, item.Segment);
}