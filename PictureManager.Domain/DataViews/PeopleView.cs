using MH.UI.Controls;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Services;
using System.Linq;

namespace PictureManager.Domain.DataViews;

public sealed class PeopleView : CollectionViewPeople {
  public void Reload() {
    var source = PersonS.GetAll().ToList();
    var groupByItems = GetGroupByItems(source).ToArray();

    Reload(source, GroupMode.ThenByRecursive, groupByItems, false);
    Root.IsExpanded = true;

    if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
      group.IsExpanded = true;
  }
}