using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class PeopleTreeCategory : TreeCategory<PersonM, CategoryGroupM> {
  private CategoryGroupM _unknownGroup;
  private const string _unknownGroupName = "Unknown";

  public CategoryGroupM UnknownGroup {
    get =>
      _unknownGroup ??=
        Items
          .OfType<CategoryGroupM>()
          .SingleOrDefault(x => x.Name.Equals(_unknownGroupName))
        ?? Core.R.CategoryGroup.ItemCreate(this, _unknownGroupName);
    set => _unknownGroup = value;
  }

  public PeopleTreeCategory(PersonR r) :
    base(Res.IconPeopleMultiple, "People", (int)Category.People) {
    DataAdapter = r;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    CanMoveItem = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<PersonM> e) {
    if (!e.Data.IsUnknown)
      TreeView.ScrollTo(e.Data, false);
  }

  public override void OnItemSelected(object o) {
    switch (o) {
      case PersonM p:
        ToggleDialogM.TogglePerson(p);
        break;
      case PeopleTreeCategory:
        Core.S.Person.OpenPeopleView();
        break;
    }
  }
}