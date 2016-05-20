using System;

namespace PictureManager.ViewModel {
  public class Keyword: BaseTreeViewTagItem {
    public int Index;
    //public int Index { get { return Data?.Idx ?? 0; } set { Data.Idx = value; } }
    public string FullPath;
    public DataModel.Keyword Data;

    public Keyword(DataModel.Keyword data) {
      Data = data;
      Id = data.Id;
      Index = data.Idx;
      IconName = "appbar_tag";
      FullPath = data.Name;
      Title = data.Name.Contains("/")
        ? data.Name.Substring(data.Name.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1)
        : data.Name;
    }
  }
}
