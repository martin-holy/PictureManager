using System;
using System.Linq;

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
        ? data.Name.Substring(data.Name.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1)
        : data.Name;
    }

    public void Sort() {
      //BUG: asi bug, takhle to asi srovnavat nejde, kdyz dam move tak se prepisou indexy a tak "i" bude odkazovat na neco jineho
      var sorted = Items.Cast<Keyword>().OrderBy(x => x.Index).ThenBy(x => x.Title).ToList();
      for (var i = 0; i < Items.Count; i++) {
        Items.Move(Items.IndexOf(Items[i]), sorted.IndexOf((Keyword) Items[i]));
      }
    }
  }
}
