using MH.UI.Controls;
using MH.Utils;
using PictureManager.Domain.Models;
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

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    public override bool GroupRecursive(CollectionViewGroup<PersonM> group) {
      if (group.RecursiveItem?.Parameter is not KeywordM keyword 
          || keyword.Items == null 
          || !keyword.Name.Equals(group.Title)) return false;

      group.IsGroupedRecursive = true;

      var groups = keyword.Items
        .Select(k => new CollectionViewGroup<PersonM>(
          group,
          null,
          k.IconName,
          k.Name,
          group.Source
            .Where(p => p?.Keywords.Contains(k) == true)
            .OrderBy(ItemOrderBy)
            .ToArray()))
        .Where(x => x.Source.Count > 0)
        .OrderBy(x => x.Title)
        .ToArray();

      if (groups.Length == 0) {
        group.IsGroupedRecursive = false;
        return false;
      }

      // TODO add group with people without any sub keyword

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title)) {
        group.GroupByItems = null;
        group.IsGroupedRecursive = false;
        return false;
      }

      group.Items.Clear();

      foreach (var g in groups) {
        group.Items.Add(g);

        if (g.GroupMode is GroupMode.ThanBy or GroupMode.ThanByRecursive)
          g.GroupByThenBy();
      }

      return true;
    }

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var all = source
        .Where(x => x.Keywords != null)
        .SelectMany(x => x.Keywords)
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Distinct()
        .ToDictionary(x => x, x => new CollectionViewGroupByItem<PersonM>(Res.IconTag, x.Name, x, ItemGroupByKeyword));
      
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

    public void Reload() {
      Root.ModeGroupRecursive = true;
      Root.UpdateSource(_peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true }));
      Root.GroupByItems = _defaultGroups;
      Root.GroupByThenBy();
      Root.IsExpanded = true;
    }

    private static string ItemGroupByGroup(PersonM item, object parameter) =>
      item.Parent == null
        ? _unknown
        : item.Parent is CategoryGroupM
          ? item.Parent.Name
          : string.Empty;

    private static string ItemGroupByKeywords(PersonM item, object parameter) =>
      item.DisplayKeywords == null
        ? string.Empty
        : string.Join(", ", item.DisplayKeywords.Select(dk => dk.Name));

    private static string ItemGroupByKeyword(PersonM item, object parameter) =>
      parameter is not KeywordM keyword
        ? string.Empty
        : item.Keywords?
          .SelectMany(x => x.GetThisAndParentRecursive())
          .Contains(keyword) == true
            ? keyword.FullName
            : string.Empty;
  }
}
