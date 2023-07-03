using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class PeopleToolsTabM : CollectionView<PersonM> {
    private readonly Core _core;
    private readonly PeopleM _peopleM;

    public HeaderedListItem<object, string> ToolsTabsItem;
    public RelayCommand<object> ReloadFromCommand { get; }

    public PeopleToolsTabM(Core core, PeopleM peopleM) {
      _core = core;
      _peopleM = peopleM;
      ToolsTabsItem = new(this, "People");
      ReloadFromCommand = new(ReloadFrom);
    }

    private void ReloadFrom() {
      var md = new MessageDialog(
        "Reload People",
        "Choice source for people to load.",
        Res.IconPeople,
        true);

      md.Buttons = new DialogButton[] {
        new("Thumbnails", null, md.SetResult(1), true),
        new("Media Viewer", null, md.SetResult(2)) };

      var result = Core.DialogHostShow(md);
      if (result < 1) return;

      switch (result) {
        case 1: Reload(PeopleM.GetFromMediaItems(_core.ThumbnailsGridsM.Current?.GetSelectedOrAll().ToArray()));
          break;
        case 2 : Reload(PeopleM.GetFromMediaItems(_core.MediaViewerM.MediaItems?.ToArray()));
          break;
      }
    }

    private void Reload(IEnumerable<PersonM> items) {
      var source = items
        .OrderBy(x => x.Name)
        .ToList();

      SetRoot(Res.IconPeopleMultiple, "People", source);
      Root.GroupMode = GroupMode.GroupByRecursive;
      Root.GroupIt();
      Root.IsExpanded = true;
    }

    // TODO change SegmentUiFullWidth to int
    public override int GetItemWidth(object item) =>
      (int)Core.Instance.SegmentsM.SegmentUiFullWidth;

    public override void Select(IEnumerable<PersonM> source, PersonM item, bool isCtrlOn, bool isShiftOn) =>
      _peopleM.Select(source.ToList(), item, isCtrlOn, isShiftOn);

    public override IEnumerable<CollectionViewGroupByItem<PersonM>> GetGroupByItems(IEnumerable<PersonM> source) {
      var src = source.ToArray();
      var top = new List<CollectionViewGroupByItem<PersonM>>();
      top.Add(GroupByItems.GetPeopleGroupsInGroupFromPeople(src));
      top.AddRange(GroupByItems.GetKeywordsFromPeople(src));

      return top;
    }

    public override string ItemOrderBy(PersonM item) =>
      item.Name;
  }
}
