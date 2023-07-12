using MH.UI.Controls;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.DataViews {
  public sealed class PeopleView : CollectionViewPeople {
    public PeopleView(PeopleM peopleM) : base(peopleM) { }

    public void Reload() {
      var source = PeopleM.GetAll(PeopleM).ToList();
      var groupByItems = GetGroupByItems(source).ToArray();

      Reload(source, GroupMode.ThenByRecursive, groupByItems, false);

      if (Root.Items.Count > 0 && Root.Items[0] is CollectionViewGroup<PersonM> group)
        group.IsExpanded = true;
    }
  }
}
