using System.Collections.Generic;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public class FolderKeyword: BaseTreeViewTagItem {
    public List<Folder> Folders = new List<Folder>();

    public FolderKeyword() {
      IconName = IconName.Folder;
    }
  }
}
