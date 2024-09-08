using MH.UI.Controls;
using PictureManager.Common.Features.Common;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PeopleVM : PersonCollectionView {
  public void Reload() {
    var source = PersonS.GetAll().ToList();
    var groupByItems = new[] { GroupByItems.GetPeopleGroupsInGroup(source) };

    Reload(source, GroupMode.ThenByRecursive, groupByItems, false);
    Root.IsExpanded = true;

    if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
      group.IsExpanded = true;
  }
}