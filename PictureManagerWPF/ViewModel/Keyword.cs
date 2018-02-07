using System;
using System.Linq;

namespace PictureManager.ViewModel {
  public class Keyword: BaseTreeViewTagItem, IDbItem {
    public DataModel.Keyword Data;

    public override string Title {
      get => Data.Name.Contains("/")
        ? Data.Name.Substring(Data.Name.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1)
        : Data.Name;
      set {
        Data.Name = value;
        OnPropertyChanged();
      }
    }

    public Keyword(DataModel.Keyword data) {
      Data = data;
      IconName = "appbar_tag";
    }

    public void Sort() {
      //BUG: asi bug, takhle to asi srovnavat nejde, kdyz dam move tak se prepisou indexy a tak "i" bude odkazovat na neco jineho
      var sorted = Items.Cast<Keyword>().OrderBy(x => x.Data.Idx).ThenBy(x => x.Title).ToList();
      for (var i = 0; i < Items.Count; i++) {
        Items.Move(Items.IndexOf(Items[i]), sorted.IndexOf((Keyword) Items[i]));
      }
    }
  }
}