using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.TreeCategories;

public sealed class FolderKeywordsTreeCategory : TreeCategory<FolderM> {
  public static RelayCommand<FolderM> SetAsFolderKeywordCommand { get; set; }

  public FolderKeywordsTreeCategory(FolderKeywordR r) :
    base(Res.IconFolderPuzzle, "Folder Keywords", (int)Category.FolderKeywords) {
    DataAdapter = r;
    SetAsFolderKeywordCommand = new(r.SetAsFolderKeyword, null, "Set as Folder Keyword");
  }

  public override void OnItemSelected(object o) =>
    Core.R.Folder.Tree.OnItemSelected(o);
}