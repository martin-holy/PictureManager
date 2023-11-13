﻿using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using System;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class KeywordsTreeCategory : TreeCategory<KeywordM, CategoryGroupM> {
  public CategoryGroupM AutoAddedGroup { get; set; }

  public KeywordsTreeCategory(KeywordsDataAdapter da) :
    base(Res.IconTagLabel, "Keywords", (int)Category.Keywords) {
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    CanMoveItem = true;
    UseTreeDelete = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<KeywordM> e) =>
    TreeView.ScrollTo(e.Data);

  public override void OnItemSelected(object o) =>
    ToggleDialogM.ToggleKeyword(o as KeywordM);

  public override bool CanDrop(object src, ITreeItem dest) =>
    base.CanDrop(src, dest) && src is ITreeItem srcItem &&
    (!(dest is ITreeGroup ? dest : dest.Parent).Items
       .Any(x => x.Name.Equals(srcItem.Name, StringComparison.OrdinalIgnoreCase)) ||
     (dest is not ITreeGroup && ReferenceEquals(srcItem.Parent, dest.Parent)));
}