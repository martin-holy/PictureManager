using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class PeopleTreeCategory : TreeCategory<PersonM, CategoryGroupM> {
  private readonly PeopleM _peopleM;

  public PeopleTreeCategory(PeopleM peopleM, PeopleDataAdapter da) :
    base(Res.IconPeopleMultiple, "People", (int)Category.People) {
    _peopleM = peopleM;
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    CanMoveItem = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<PersonM> e) =>
    TreeView.ScrollTo(e.Data);

  public override void OnItemSelected(object o) {
    switch (o) {
      case PersonM p:
        ToggleDialogM.TogglePerson(p);
        break;
      case PeopleTreeCategory:
        _peopleM.OpenPeopleView();
        break;
    }
  }
}