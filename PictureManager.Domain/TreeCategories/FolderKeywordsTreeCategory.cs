using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class FolderKeywordsTreeCategory : TreeCategory<FolderM> {
  public static RelayCommand<FolderM> SetAsFolderKeywordCommand =>
    new(Core.Db.FolderKeywords.SetAsFolderKeyword);

  public FolderKeywordsTreeCategory() : base(Res.IconFolderPuzzle, "Folder Keywords", (int)Category.FolderKeywords) {
    DataAdapter = Core.Db.FolderKeywords = new(this);
  }

  public override void OnItemSelected(object o) =>
    Core.FoldersM.TreeCategory.OnItemSelected(o);
}