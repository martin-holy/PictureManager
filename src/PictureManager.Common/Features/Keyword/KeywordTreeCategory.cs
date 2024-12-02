using MH.UI.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using System;
using System.Linq;

namespace PictureManager.Common.Features.Keyword;

public sealed class KeywordTreeCategory : TreeCategory<KeywordM, CategoryGroupM> {
  public CategoryGroupM AutoAddedGroup { get; set; } = null!;

  public KeywordTreeCategory(KeywordR r, CategoryGroupR groupR)
    : base(new(), Res.IconTagLabel, "Keywords", (int)Category.Keywords, r, groupR) {
    CanMoveItem = true;
    UseTreeDelete = true;
    ScrollToAfterCreate = true;
  }

  protected override void _onItemSelected(object o) =>
    Core.VM.ToggleDialog.Toggle(o as KeywordM);

  public override bool CanDrop(object? src, ITreeItem? dest) =>
    base.CanDrop(src, dest) && src is ITreeItem srcItem &&
    (!(dest is ITreeGroup ? dest : dest!.Parent!).Items
       .Any(x => x.Name.Equals(srcItem.Name, StringComparison.OrdinalIgnoreCase)) ||
     (dest is not ITreeGroup && ReferenceEquals(srcItem.Parent, dest.Parent)));
}