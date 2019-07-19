using System.Collections.Generic;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class FavoriteFolders : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<FavoriteFolder> All { get; } = new List<FavoriteFolder>();

    public FavoriteFolders() : base(Category.FavoriteFolders) {
      Title = "Favorites";
      IconName = IconName.FolderStar;
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void ClearBeforeLoad() {
      All.Clear();
    }

    public void NewFromCsv(string csv) {
      // ID|Folder
      var props = csv.Split('|');
      if (props.Length != 2) return;
      var id = int.Parse(props[0]);
      AddRecord(new FavoriteFolder(id) {Csv = props});
    }

    public void LinkReferences() {
      foreach (var item in All) {
        item.Folder = ACore.Folders.AllDic[int.Parse(item.Csv[1])];
        item.Title = item.Folder.Title;
        Items.Add(item);

        // csv array is not needed any more
        item.Csv = null;
      }
    }

    private void AddRecord(FavoriteFolder record) {
      All.Add(record);
    }

    public void Remove(FavoriteFolder folder) {
      Items.Remove(folder);
      All.Remove(folder);
      Helper.IsModifed = true;
    }

    public void Add(Folder folder) {
      var ff = new FavoriteFolder(Helper.GetNextId()) {
        Title = folder.Title,
        Folder = folder
      };
      AddRecord(ff);
      Items.Add(ff);
    }
  }
}
