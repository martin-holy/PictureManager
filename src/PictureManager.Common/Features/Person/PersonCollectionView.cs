using MH.UI.Controls;
using MH.Utils.EventsArgs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Segment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public class PersonCollectionView : CollectionView<PersonM> {
  private static readonly IReadOnlyList<SortField<PersonM>> _sortFields = [
    new SortField<PersonM>("Name", x => x.Name, StringComparer.CurrentCultureIgnoreCase)
  ];

  public PersonCollectionView() : base(Res.IconPeopleMultiple, "People", [ViewMode.ThumbSmall, ViewMode.List, ViewMode.Tiles]) {
    DefaultSortField = _sortFields[0];
  }

  public override IEnumerable<GroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<PersonM>>();
    top.Add(GroupByItems.GetPeopleGroupsInGroup(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.AddRange(GroupByItems.GetFolders(src));

    return top;
  }

  public override int GetItemSize(ViewMode viewMode, PersonM item, bool getWidth) =>
    viewMode switch {
      ViewMode.List => getWidth ? PersonVM.PersonListWidth : PersonVM.PersonListHeight,
      ViewMode.Tiles => getWidth ? PersonVM.PersonTileTextWidth + SegmentVM.SegmentUiFullWidth : SegmentVM.SegmentUiFullWidth,
      _ => SegmentVM.SegmentUiFullWidth
    };

  public override IEnumerable<SortField<PersonM>> GetSortFields() => _sortFields;

  public override int SortCompare(PersonM itemA, PersonM itemB) =>
    string.Compare(itemA.Name, itemB.Name, StringComparison.CurrentCultureIgnoreCase);

  protected override void _onItemSelected(SelectionEventArgs<PersonM> e) =>
    Core.S.Person.Select(e);

  protected override void _onItemOpened(PersonM item) =>
    Core.S.Segment.ViewMediaItemsWithSegment(this, item.Segment);

  public override string GetItemTemplateName(ViewMode viewMode) =>
    viewMode switch {
      ViewMode.List => "PM.DT.Person.ListItem",
      ViewMode.Tiles => "PM.DT.Person.Tile",
      _ => "PM.DT.Person.Thumb"
    };
}