using MH.UI.Controls;
using MH.Utils;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels {
  public sealed class PeopleVM : CollectionView<PersonM> {
    private static readonly CategoryGroupM _unknownGroup = new(-1, "Unknown", Category.People, Res.IconPeopleMultiple);
    private readonly PeopleM _peopleM;

    public PeopleVM(PeopleM peopleM) {
      _peopleM = peopleM;
    }

    public void Reload() {
      var source = _peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true })
        .OrderBy(x => x.Name);
      SetRoot(Res.IconPeopleMultiple, "People", source);
      Root.GroupMode = GroupMode.GroupByRecursive;
      Root.GroupByItems = new [] { GetPeopleGroups(Root.Source) };
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
      var top = new List<CollectionViewGroupByItem<PersonM>> { GetPeopleGroups(src) };
      var all = src
        .Where(x => x.Keywords != null)
        .SelectMany(x => x.Keywords)
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Distinct()
        .ToDictionary(x => x, x => new CollectionViewGroupByItem<PersonM>(
          Res.IconTag, x.Name, x, GroupItemByKeyword));

      foreach (var item in all.OrderBy(x => x.Key.FullName)) {
        if (item.Key.Parent is not KeywordM parent) {
          top.Add(item.Value);
          continue;
        }

        all[parent].AddItem(item.Value);
      }

      return top;
    }

    public override string ItemOrderBy(PersonM item) =>
      item.Name;

    private static CollectionViewGroupByItem<PersonM> GetPeopleGroups(IEnumerable<PersonM> people) {
      var groupItems = people
        .GroupBy(x => x.Parent)
        .Select(x => x.Key == null
          ? new CollectionViewGroupByItem<PersonM>(Res.IconPeopleMultiple, _unknownGroup.Name, _unknownGroup, GroupItemByGroup)
          : new CollectionViewGroupByItem<PersonM>(Res.IconPeopleMultiple, x.Key.Name, x.Key, GroupItemByGroup))
        .OrderBy(x => x.Name);

      var root = new CollectionViewGroupByItem<PersonM>(Res.IconPeopleMultiple, "Groups", null, GroupItemByGroup);

      foreach (var groupItem in groupItems)
        root.AddItem(groupItem);

      return root;
    }

    private static bool GroupItemByGroup(PersonM item, object parameter) =>
      parameter == null
      || ReferenceEquals(parameter, _unknownGroup) && item.Parent == null
      || ReferenceEquals(parameter, item.Parent);

    private static bool GroupItemByKeyword(PersonM item, object parameter) =>
      item.Keywords != null
      && parameter is KeywordM keyword
      && item.Keywords
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Contains(keyword);
  }
}
