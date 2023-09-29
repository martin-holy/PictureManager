using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;

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
}