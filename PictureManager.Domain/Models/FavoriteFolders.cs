using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolders : BaseCatTreeViewCategory, ITable, ICatTreeViewCategory {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new List<IRecord>();

    public FavoriteFolders() : base(Category.FavoriteFolders) {
      Title = "Favorites";
      IconName = IconName.FolderStar;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|FolderId|Title
      var props = csv.Split('|');
      if (props.Length != 3) return;
      var id = int.Parse(props[0]);
      All.Add(new FavoriteFolder(id) {Title = props[2], Csv = props});
    }

    public void LinkReferences() {
      foreach (var item in All.Cast<FavoriteFolder>()) {
        item.Folder = Core.Instance.Folders.AllDic[int.Parse(item.Csv[1])];
        item.ToolTip = item.Folder.FullPath;
        item.Parent = this;
        Items.Add(item);

        // csv array is not needed any more
        item.Csv = null;
      }
    }

    public void ItemCreate(Folder folder) {
      var ff = new FavoriteFolder(Helper.GetNextId()) {
        Title = folder.Title,
        Folder = folder,
        Parent = this
      };

      var idx = CatTreeViewUtils.SetItemInPlace(this, ff);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, this, idx);
      
      All.Insert(allIdx, ff);
      Core.Instance.Sdb.SetModified<FavoriteFolders>();
      Core.Instance.Sdb.SaveIdSequences();
    }

    public new void ItemDelete(ICatTreeViewItem item) {
      if (!(item is FavoriteFolder folder)) return;

      // remove from the Tree
      item.Parent.Items.Remove(item);

      // remove from DB
      All.Remove(folder);

      folder.Folder = null;
      Core.Instance.Sdb.SetModified<FavoriteFolders>();
    }
  }
}
