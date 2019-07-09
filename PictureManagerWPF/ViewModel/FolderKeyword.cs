using System.Collections.Generic;

namespace PictureManager.ViewModel {
  public class FolderKeyword: BaseTreeViewTagItem {
    public List<Database.Folder> Folders = new List<Database.Folder>();

    public FolderKeyword() {
      IconName = IconName.Folder;
    }
  }
}
