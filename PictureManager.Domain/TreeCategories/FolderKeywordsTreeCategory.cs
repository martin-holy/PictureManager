using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class FolderKeywordsTreeCategory : TreeCategory<FolderM> {
  public static RelayCommand<FolderM> SetAsFolderKeywordCommand =>
    new(Core.Db.FolderKeywords.SetAsFolderKeyword);

  public FolderKeywordsTreeCategory(FolderKeywordsDataAdapter da) :
    base(Res.IconFolderPuzzle, "Folder Keywords", (int)Category.FolderKeywords) {
    DataAdapter = da;
  }

  public override void OnItemSelected(object o) =>
    Core.FoldersM.TreeCategory.OnItemSelected(o);
}