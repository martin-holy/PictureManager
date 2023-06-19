using MH.UI.Controls;
using MH.Utils;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels {
  public sealed class PeopleVM : CollectionView<PersonM> {
    private const string _unknown = "Unknown";
    private readonly PeopleM _peopleM;

    private static readonly CollectionViewGroupByItem<PersonM>[] _defaultGroups = {
      new(Res.IconPeopleMultiple, "Group", null, ItemGroupByGroup),
      new(Res.IconTagLabel, "Keywords", null, ItemGroupByKeywords)
    };

    public PeopleVM(PeopleM peopleM) : base(Res.IconPeopleMultiple, "People") {
      _peopleM = peopleM;
    }

    public void Reload() {
      Root.UpdateSource(_peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true }));
      Root.GroupMode = GroupMode.ThanByRecursive;
      Root.GroupByItems = _defaultGroups;
      Root.RecursiveItem = Root.GroupByItems[0];
      Root.GroupByThenBy();
      Root.IsExpanded = true;
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var all = source
        .Where(x => x.Keywords != null)
        .SelectMany(x => x.Keywords)
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Distinct()
        .ToDictionary(x => x, x => new CollectionViewGroupByItem<PersonM>(
          Res.IconTag, x.Name, x, ItemGroupByKeyword));
      
      var top = new List<CollectionViewGroupByItem<PersonM>>();

      foreach (var item in all.OrderBy(x => x.Key.FullName)) {
        if (item.Key.Parent is not KeywordM parent) {
          top.Add(item.Value);
          continue;
        }

        var groupItem = all[parent];
        item.Value.Parent = groupItem;
        groupItem.Items.Add(item.Value);
      }

      foreach (var item in _defaultGroups)
        item.IsSelected = false;

      return _defaultGroups.Concat(top);
    }

    public override string ItemOrderBy(PersonM item) =>
      item.Name;

    private static Tuple<object, string>[] ItemGroupByGroup(PersonM item, object parameter, bool isRecursive) =>
      isRecursive
        ? null
        : item.Parent == null
          ? new Tuple<object, string>[] { new(null, _unknown) }
          : item.Parent is CategoryGroupM cg
            ? new Tuple<object, string>[] { new(cg, cg.Name) }
            : null;

    private static Tuple<object, string>[] ItemGroupByKeywords(PersonM item, object parameter, bool isRecursive) =>
      isRecursive
        ? null
        : item.DisplayKeywords == null
          ? null
          : new Tuple<object, string>[] { new(null, string.Join(", ", item.DisplayKeywords.Select(dk => dk.Name))) };

    private static Tuple<object, string>[] ItemGroupByKeyword(PersonM item, object parameter, bool isRecursive) {
      if (item.Keywords == null || parameter is not KeywordM keyword) return null;

      var keywords = item.Keywords.SelectMany(x => x.GetThisAndParentRecursive());

      if (!isRecursive)
        return keywords.Contains(keyword)
          ? new Tuple<object, string>[] { new(keyword, keyword.Name) }
          : null;

      return keywords
        .Where(x => x.Parent.Equals(keyword))
        .Select(x => new Tuple<object, string>(x, x.Name))
        .ToArray();
    }
  }
}
