using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PictureManager.Data {
  public class FolderKeyword: BaseTagItem {
    public string FullPath;
    public ObservableCollection<FolderKeyword> Items { get; set; }
    public FolderKeyword Parent;
    public string FolderIds; 

    public FolderKeyword() {
      Items = new ObservableCollection<FolderKeyword>();
    }
  }
}
