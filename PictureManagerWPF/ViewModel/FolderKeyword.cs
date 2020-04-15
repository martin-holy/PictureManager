using System.Collections.Generic;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public class FolderKeyword: BaseTreeViewTagItem {
    public List<Folder> Folders { get; } = new List<Folder>();

    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) Folders.ForEach(x => x.LoadSubFolders(false));
      }
    }

    public FolderKeyword() {
      IconName = IconName.Folder;
    }
  }
}
