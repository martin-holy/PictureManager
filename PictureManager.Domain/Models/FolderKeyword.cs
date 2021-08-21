using PictureManager.Domain.CatTreeViewModels;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeyword : CatTreeViewItem, ICatTreeViewTagItem {
    public List<Folder> Folders { get; } = new();

    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) Folders.ForEach(x => x.LoadSubFolders(false));
      }
    }

    public FolderKeyword() {
      IconName = IconName.FolderPuzzle;
    }
  }
}
