using MH.UI.Controls;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Services;
using System.Linq;

namespace PictureManager.Domain.ViewModels;

public sealed class PeopleVM : CollectionViewPeople {
  public void Reload() {
    var source = PersonS.GetAll().ToList();
    var groupByItems = new[] { GroupByItems.GetPeopleGroupsInGroup(source) };

    Reload(source, GroupMode.ThenByRecursive, groupByItems, false);
    Root.IsExpanded = true;

    if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
      group.IsExpanded = true;
  }
}