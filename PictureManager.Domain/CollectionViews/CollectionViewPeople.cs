using MH.UI.Controls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews; 

public class CollectionViewPeople : CollectionView<PersonM> {
  public PeopleM PeopleM { get; }

  public CollectionViewPeople(PeopleM peopleM) {
    PeopleM = peopleM;
    Icon = Res.IconPeopleMultiple;
    Name = "People";
  }

  public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
    var src = source.ToArray();
    var top = new List<CollectionViewGroupByItem<PersonM>>();
    top.Add(GroupByItems.GetPeopleGroupsInGroupFromPeople(src));
    top.Add(GroupByItems.GetKeywordsInGroupFromPeople(src));

    return top;
  }

  public override int GetItemWidth(PersonM item) =>
    SegmentsM.SegmentUiFullWidth;

  public override int SortCompare(PersonM itemA, PersonM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  public override void OnSelectItem(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
    PeopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);
}