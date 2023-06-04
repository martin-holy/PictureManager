using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels {
  public sealed class PeopleVM : CollectionView {
    private const string _unknown = "Unknown";
    private readonly PeopleM _peopleM;

    public PeopleVM(PeopleM peopleM) {
      _peopleM = peopleM;

      Root = new(null, Res.IconPeopleMultiple, "People", null) { View = this };
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<object> source, object item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.OfType<PersonM>().ToList(), (PersonM)item, isCtrlOn, isShiftOn);

    public override IEnumerable<object> GetGroupByItems(IEnumerable<object> source) =>
      new[] {
        new ListItem<object>(null, Res.IconPeopleMultiple, "Group"),
        new ListItem<object>(null, Res.IconTagLabel, "Keywords")
      }.Concat(
        source
          .Cast<PersonM>()
          .Where(x => x.Keywords != null)
          .SelectMany(x => x.Keywords)
          .SelectMany(x => x.GetThisAndParentRecursive())
          .Distinct()
          .OrderBy(x => x.FullName)
          .Select(x => new ListItem<object>(x, Res.IconTag, x.FullName)));

    public override string ItemGroupBy(CollectionViewGroup group, object item) {
      if (group.GroupBy.Name == "Group")
        return ItemGroupByGroup(item as PersonM);

      if (group.GroupBy.Name == "Keywords")
        return ItemGroupByKeywords(item as PersonM);

      return ItemGroupByKeyword(item as PersonM, group.GroupBy.Content as KeywordM);
    }

    public override IOrderedEnumerable<CollectionViewGroup> SourceGroupBy(CollectionViewGroup group) {
      if (group.GroupBy.Name == "Group")
        return SourceGroupByGroup(group);

      if (group.GroupBy.Name == "Keywords")
        return SourceGroupByKeywords(group);

      return SourceGroupByKeyword(group, group.GroupBy.Content as KeywordM);
    }

    public void Reload() {
      Root.UpdateSource(_peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true }));

      GroupIt(
        Root,
        new() {
          new(null, Res.IconPeopleMultiple, "Group"),
          new(null, Res.IconTagLabel, "Keywords")
        });
      Root.IsExpanded = true;
    }

    private static string ItemGroupByGroup(PersonM item) =>
      item.Parent == null
        ? _unknown
        : item.Parent is CategoryGroupM
          ? item.Parent.Name
          : string.Empty;

    private static string ItemGroupByKeywords(PersonM item) =>
      item.DisplayKeywords == null
        ? string.Empty
        : string.Join(", ", item.DisplayKeywords.Select(dk => dk.Name));

    private static string ItemGroupByKeyword(PersonM item, KeywordM keyword) =>
      item.Keywords?
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Contains(keyword) == true
          ? keyword.FullName
          : string.Empty;

    private static IOrderedEnumerable<CollectionViewGroup> SourceGroupByKeyword(CollectionViewGroup group, KeywordM keyword) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(x => ItemGroupByKeyword(x, keyword))
        .Select(x => new CollectionViewGroup(group, Res.IconTag, x.Key, x))
        .OrderBy(x => x.Title);

    private static IOrderedEnumerable<CollectionViewGroup> SourceGroupByKeywords(CollectionViewGroup group) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(ItemGroupByKeywords)
        .Select(x => new CollectionViewGroup(group, Res.IconTagLabel, x.Key, x))
        .OrderBy(x => x.Title);

    private static IOrderedEnumerable<CollectionViewGroup> SourceGroupByGroup(CollectionViewGroup group) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(ItemGroupByGroup)
        .Select(x => new CollectionViewGroup(group, Res.IconPeopleMultiple, x.Key, x))
        .OrderBy(x => x.Title, Comparer<string>.Create((a, b) => {
          if (_unknown.Equals(a)) return 1;
          if (_unknown.Equals(b)) return -1;
          if (string.Empty.Equals(a)) return _unknown.Equals(b) ? -1 : 1;
          if (string.Empty.Equals(b)) return _unknown.Equals(a) ? 1 : -1;

          return StringComparer.CurrentCulture.Compare(a, b);
        }));
  }
}
