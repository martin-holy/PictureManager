using MH.Utils;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels {
  public sealed class PeopleVM : CollectionView<PersonM> {
    private const string _unknown = "Unknown";
    private readonly PeopleM _peopleM;

    private static readonly List<CollectionViewGroupByItem<PersonM>> _defaultGroups = new() {
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

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) =>
      _defaultGroups
        .Concat(source
          .Where(x => x.Keywords != null)
          .SelectMany(x => x.Keywords)
          .SelectMany(x => x.GetThisAndParentRecursive())
          .Distinct()
          .OrderBy(x => x.FullName)
          .Select(x => new CollectionViewGroupByItem<PersonM>(Res.IconTag, x.FullName, x, ItemGroupByKeyword)));

    public override string ItemOrderBy(PersonM item) =>
      item.Name;

    public void Reload() {
      Root.UpdateSource(_peopleM.DataAdapter.All
        .Where(x => x.Parent is not CategoryGroupM { IsHidden: true }));

      GroupIt(Root, _defaultGroups);
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
