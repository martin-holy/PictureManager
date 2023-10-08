using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class PeopleTreeCategory : TreeCategory<PersonM, CategoryGroupM> {
  private readonly PeopleM _peopleM;
  private CategoryGroupM _unknownGroup;
  private const string _unknownGroupName = "Unknown";

  public CategoryGroupM UnknownGroup {
    get =>
      _unknownGroup ??=
        Items
          .OfType<CategoryGroupM>()
          .SingleOrDefault(x => x.Name.Equals(_unknownGroupName))
        ?? Core.Db.CategoryGroups.ItemCreate(this, _unknownGroupName);
    set => _unknownGroup = value;
  }

  public PeopleTreeCategory(PeopleM peopleM, PeopleDataAdapter da) :
    base(Res.IconPeopleMultiple, "People", (int)Category.People) {
    _peopleM = peopleM;
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    CanMoveItem = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<PersonM> e) {
    if (e.Data.Id > 0)
      TreeView.ScrollTo(e.Data);
  }

  public override void OnItemSelected(object o) {
    switch (o) {
      case PersonM p:
        if (p.Id > 0)
          ToggleDialogM.TogglePerson(p);
        break;
      case PeopleTreeCategory:
        _peopleM.OpenPeopleView();
        break;
    }
  }
}