using System.Collections.Generic;

namespace PictureManager.ViewModel {
  public class FolderKeyword: BaseTreeViewTagItem {
    public string FullPath;
    public List<int> FolderIdList;
    public string FolderIds => string.Join(",", FolderIdList);

    public FolderKeyword() {
      FolderIdList = new List<int>();
      IconName = IconName.Folder;
    }
  }
}
