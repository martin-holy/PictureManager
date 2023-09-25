using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories; 

public sealed class KeywordsTreeCategory : TreeCategory<KeywordM, CategoryGroupM> {
  public CategoryGroupM AutoAddedGroup { get; set; }

  public KeywordsTreeCategory() : base(Res.IconTagLabel, "Keywords", (int)Category.Keywords) {
    DataAdapter = Core.Db.Keywords = new(this);
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    CanMoveItem = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<KeywordM> e) =>
    TreeView.ScrollTo(e.Data);

  public override void OnItemSelected(object o) =>
    ToggleDialogM.ToggleKeyword(o as KeywordM);
}