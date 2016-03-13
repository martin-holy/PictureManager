using System;
using System.Collections.ObjectModel;

namespace PictureManager.ViewModel {
  public class Keyword: BaseTreeViewTagItem {
    public long Index;
    public string FullPath;
    public DataModel.Keyword Data;
    public ObservableCollection<Keyword> Items { get; set; }
    public Keyword Parent;

    public Keyword() {
      Items = new ObservableCollection<Keyword>();
    }

    public Keyword(DataModel.Keyword data) : this() {
      Data = data;
      Id = data.Id;
      Index = data.Idx;
      IconName = "appbar_tag";
      FullPath = data.Name;
    }
  }
}
