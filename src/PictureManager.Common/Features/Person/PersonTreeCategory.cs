using MH.UI.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PersonTreeCategory : TreeCategory<PersonM, CategoryGroupM> {
  private CategoryGroupM? _unknownGroup;
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

  public PersonTreeCategory(PersonR r, CategoryGroupR groupR)
    : base(new(), Res.IconPeopleMultiple, "People", (int)Category.People, r, groupR) {
    CanMoveItem = true;
    ScrollToAfterCreate = true;
  }

  protected override void _onItemSelected(object o) {
    switch (o) {
      case PersonM p:
        _ = Core.VM.ToggleDialog.Toggle(p);
        break;
      case PersonTreeCategory:
        Core.VM.OpenPeopleView();
        break;
    }
  }

  public override IEnumerable<ITreeItem> GroupGetItemsToMove(ITreeGroup group) =>
    Core.S.Person.Selected.Items.Where(x => !ReferenceEquals(x.Parent, group));

  public override bool GroupAnyItemsToMove(ITreeGroup group) =>
    Core.S.Person.Selected.Items.Where(x => !ReferenceEquals(x.Parent, group)).Any();
}