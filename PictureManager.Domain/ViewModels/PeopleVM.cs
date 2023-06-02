using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

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

    public override IEnumerable<object> GetGroupByItems(IEnumerable<object> source) =>
      new[] {
        new ListItem<object>(null, Res.IconPeopleMultiple, "Group"),
        new ListItem<object>(null, Res.IconTagLabel, "Keywords")
      }.Concat(
        source
          .Cast<PersonM>()
          .Where(x => x.Keywords != null)
          .SelectMany(x => x.Keywords)
          .Distinct()
          .OrderBy(x => x.FullName)
          .Select(x => new ListItem<object>(x, Res.IconTag, x.FullName)));

    public override IOrderedEnumerable<CollectionViewGroup> GroupByItem(CollectionViewGroup group, ListItem<object> item) {
      if (item.Name == "Group")
        return GroupByGroup(group);
      
      if (item.Name == "Keywords")
        return GroupByKeywords(group);

      return GroupByKeyword(group, item.Content as KeywordM);
    }

    public override void Select(IEnumerable<object> source, object item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.OfType<PersonM>().ToList(), (PersonM)item, isCtrlOn, isShiftOn);

    public void Reload() {
      Root.UpdateSource(_peopleM.DataAdapter.All);
      GroupIt(
        Root,
        new() {
          new(null, Res.IconPeopleMultiple, "Group"),
          new(null, Res.IconTagLabel, "Keywords")
        });
      Root.IsExpanded = true;
    }

    private static IOrderedEnumerable<CollectionViewGroup> GroupByKeyword(CollectionViewGroup group, KeywordM keyword) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(x => x.Keywords == null
          ? string.Empty
          : x.Keywords.Contains(keyword)
            ? keyword.FullName
            : string.Empty)
        .Select(x => new CollectionViewGroup(group, Res.IconTag, x.Key, x))
        .OrderBy(x => x.Title);

    private static IOrderedEnumerable<CollectionViewGroup> GroupByKeywords(CollectionViewGroup group) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(x => x.DisplayKeywords == null
          ? string.Empty
          : string.Join(", ", x.DisplayKeywords.Select(dk => dk.Name)))
        .Select(x => new CollectionViewGroup(group, Res.IconTagLabel, x.Key, x))
        .OrderBy(x => x.Title);

    private static IOrderedEnumerable<CollectionViewGroup> GroupByGroup(CollectionViewGroup group) =>
      group.Source
        .Cast<PersonM>()
        .OrderBy(x => x.Name)
        .GroupBy(x => x.Parent)
        .Where(x => x.Key is not CategoryGroupM { IsHidden: true })
        .Select(x => new CollectionViewGroup(
          group,
          Res.IconPeopleMultiple,
          x.Key == null
            ? _unknown
            : x.Key is CategoryGroupM
              ? x.Key.Name
              : string.Empty,
          x))
        .OrderBy(x => x.Title, Comparer<string>.Create((a, b) => {
          if (_unknown.Equals(a)) return 1;
          if (_unknown.Equals(b)) return -1;
          if (string.Empty.Equals(a)) return _unknown.Equals(b) ? -1 : 1;
          if (string.Empty.Equals(b)) return _unknown.Equals(a) ? 1 : -1;

          return StringComparer.CurrentCulture.Compare(a, b);
        }));
  }
}
