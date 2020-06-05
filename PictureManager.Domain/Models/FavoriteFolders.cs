using System.Collections.Generic;
using SimpleDB;

namespace PictureManager.Domain.Models {
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

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
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
        item.Folder = Core.Instance.Folders.AllDic[int.Parse(item.Csv[1])];
        item.Title = item.Folder.Title;
        item.ToolTip = item.Folder.FullPath;
        Items.Add(item);

        // csv array is not needed any more
        item.Csv = null;
      }
    }

    private void AddRecord(FavoriteFolder record) {
      All.Add(record);
    }

    public void Remove(FavoriteFolder folder) {
      // remove from the Tree
      Items.Remove(folder);
      
      // remove from DB
      All.Remove(folder);

      folder.Folder = null;
      Helper.IsModified = true;
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
