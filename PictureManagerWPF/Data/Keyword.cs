using System;
using System.Collections.ObjectModel;

namespace PictureManager.Data {
  public class Keyword: BaseTagItem {
    public int Index;
    public string FullPath;
    public ObservableCollection<Keyword> Items { get; set; }
    public Keyword Parent;

    public Keyword() {
      Items = new ObservableCollection<Keyword>();
    }

    public void Rename(DbStuff db, string newName) {
      if (Items.Count != 0) return;
      FullPath = FullPath.Contains("/")
        ? FullPath.Substring(0, FullPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1) + newName
        : newName;
      Title = newName;
      db.Execute($"update Keywords set Keyword = \"{FullPath}\" where Id = {Id}");
    }
  }
}
