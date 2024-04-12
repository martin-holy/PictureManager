using MH.UI.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using System;
using System.Linq;

namespace PictureManager.Common.TreeCategories;

public sealed class KeywordsTreeCategory : TreeCategory<KeywordM, CategoryGroupM> {
  public CategoryGroupM AutoAddedGroup { get; set; }

  public KeywordsTreeCategory(KeywordR r) : base(Res.IconTagLabel, "Keywords", (int)Category.Keywords) {
    DataAdapter = r;
    CanMoveItem = true;
    UseTreeDelete = true;
    ScrollToAfterCreate = true;
  }

  public override void OnItemSelected(object o) =>
    Core.VM.ToggleDialog.Toggle(o as KeywordM);

  public override bool CanDrop(object src, ITreeItem dest) =>
    base.CanDrop(src, dest) && src is ITreeItem srcItem &&
    (!(dest is ITreeGroup ? dest : dest.Parent).Items
       .Any(x => x.Name.Equals(srcItem.Name, StringComparison.OrdinalIgnoreCase)) ||
     (dest is not ITreeGroup && ReferenceEquals(srcItem.Parent, dest.Parent)));
}