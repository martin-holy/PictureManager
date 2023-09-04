using MH.UI.Controls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public class CollectionViewPeople : CollectionView<PersonM> {
    public PeopleM PeopleM { get; }

    public CollectionViewPeople(PeopleM peopleM) {
      PeopleM = peopleM;
      Icon = Res.IconPeopleMultiple;
      Name = "People";
    }

    public void Reload(List<PersonM> source, GroupMode groupMode, CollectionViewGroupByItem<PersonM>[] groupByItems, bool expandAll) {
      SetRoot(new CollectionViewGroup<PersonM>(source, this, groupMode, groupByItems), expandAll);
    }

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<PersonM>>();
      top.Add(GroupByItems.GetPeopleGroupsInGroupFromPeople(src));
      top.Add(GroupByItems.GetKeywordsInGroupFromPeople(src));

      return top;
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(PersonM item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override int SortCompare(PersonM itemA, PersonM itemB) =>
      string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

    public override void OnSelectItem(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      PeopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);
  }
}
