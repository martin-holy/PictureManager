using MH.UI.Controls;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public class CollectionViewPeople : CollectionView<PersonM> {
    public PeopleM PeopleM { get; }

    public CollectionViewPeople(PeopleM peopleM) {
      PeopleM = peopleM;
    }

    public void Reload(List<PersonM> source, GroupMode groupMode, CollectionViewGroupByItem<PersonM>[] groupByItems, bool expandAll) {
      SetRoot(new(source, Res.IconPeopleMultiple, "People", this, groupMode, groupByItems), expandAll);
    }

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<PersonM>>();
      top.Add(GroupByItems.GetPeopleGroupsInGroupFromPeople(src));
      top.Add(GroupByItems.GetKeywordsInGroupFromPeople(src));

      return top;
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override string ItemOrderBy(PersonM item) =>
      item.Name;

    public override void Select(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      PeopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);
  }
}
