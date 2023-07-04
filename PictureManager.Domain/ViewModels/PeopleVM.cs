using MH.UI.Controls;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels {
  public sealed class PeopleVM : CollectionView<PersonM> {
    private readonly PeopleM _peopleM;

    public PeopleVM(PeopleM peopleM) {
      _peopleM = peopleM;
    }

    public void Reload() {
      var source = _peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true })
        .OrderBy(x => x.Name)
        .ToList();

      SetRoot(Res.IconPeopleMultiple, "People", source);
      Root.GroupMode = GroupMode.GroupByRecursive;
      Root.GroupByItems = new [] { GroupByItems.GetPeopleGroupsInGroupFromPeople(Root.Source) };
      Root.GroupIt();
      Root.IsExpanded = true;

      if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
        group.IsExpanded = true;
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<PersonM>> { GroupByItems.GetPeopleGroupsInGroupFromPeople(src) };
      top.AddRange(GroupByItems.GetKeywordsFromPeople(src));

      return top;
    }

    public override string ItemOrderBy(PersonM item) =>
      item.Name;
  }
}
