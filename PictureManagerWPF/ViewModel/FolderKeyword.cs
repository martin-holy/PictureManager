using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PictureManager.ViewModel {
  public class FolderKeyword: BaseTreeViewTagItem {
    public string FullPath;
    public ObservableCollection<FolderKeyword> Items { get; set; }
    public FolderKeyword Parent;
    public List<long> FolderIdList;
    public string FolderIds => string.Join(",", FolderIdList);

    public FolderKeyword() {
      Items = new ObservableCollection<FolderKeyword>();
      FolderIdList = new List<long>();
    }
  }
}
