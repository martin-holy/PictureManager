using PictureManager.Domain.CatTreeViewModels;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeyword : CatTreeViewItem, ICatTreeViewTagItem {
    public List<Folder> Folders { get; } = new();

    public FolderKeyword() {
      IconName = IconName.FolderPuzzle;
      OnExpand += (o, e) => Folders.ForEach(x => x.LoadSubFolders(false));
    }
  }
}
