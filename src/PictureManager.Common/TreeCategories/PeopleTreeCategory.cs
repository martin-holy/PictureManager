﻿using MH.UI.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using System.Linq;

namespace PictureManager.Common.TreeCategories;

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

  public PeopleTreeCategory(PersonR r) : base(Res.IconPeopleMultiple, "People", (int)Category.People) {
    DataAdapter = r;
    CanMoveItem = true;
    ScrollToAfterCreate = true;
  }

  public override void OnItemSelected(object o) {
    switch (o) {
      case PersonM p:
        Core.VM.ToggleDialog.Toggle(p);
        break;
      case PeopleTreeCategory:
        Core.VM.OpenPeopleView();
        break;
    }
  }
}