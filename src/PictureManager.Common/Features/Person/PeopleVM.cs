using MH.UI.Controls;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Common;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PeopleVM : PersonCollectionView {
  public void Reload() {
    var source = Core.R.Person.All.Where(x => x.Parent is not CategoryGroupM { IsHidden: true }).ToList();
    var groupByItems = new[] { GroupByItems.GetPeopleGroupsInGroup(source) };

    Reload(source, GroupMode.ThenByRecursive, groupByItems, false, true);
    Root.IsExpanded = true;

    if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
      group.IsExpanded = true;
  }
}