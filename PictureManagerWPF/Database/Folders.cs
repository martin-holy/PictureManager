using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Folders : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<Folder> All { get; } = new List<Folder>();
    public Dictionary<int, Folder> AllDic { get; } = new Dictionary<int, Folder>();

    public Folders() : base(Category.Folders) {
      Title = "Folders";
      IconName = IconName.Folder;
    }

    public void NewFromCsv(string csv) {
      // ID|Name|Parent|IsFolderKeyword
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      AddRecord(new Folder(id, props[1], null) {Csv = props, IsFolderKeyword = props[3] == "1"});
    }

    public void LinkReferences() {
      // ID|Name|Parent|IsFolderKeyword
      foreach (var folder in All) {
        // reference to Parent and back reference from Parent to SubFolder
        if (folder.Csv[2] != string.Empty) {
          folder.Parent = AllDic[int.Parse(folder.Csv[2])];
          folder.Parent.Items.Add(folder);
        }

        // csv array is not needed any more
        folder.Csv = null;
      }

      foreach (var topFolder in All.Where(x => x.Parent == null)) {
        Items.Add(topFolder);
      }

      AddDrives();
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void ClearBeforeLoad() {
      All.Clear();
      AllDic.Clear();
    }

    public void AddRecord(Folder record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public void DeleteRecord(Folder folder) {
      All.Remove(folder);
      AllDic.Remove(folder.Id);

      // remove MediaItems
      folder.MediaItems.ForEach(mi => ACore.MediaItems.Delete(mi));

      // remove FavoriteFolder
      var ff = ACore.FavoriteFolders.All.SingleOrDefault(x => x.Folder.Equals(folder));
      if (ff != null) ACore.FavoriteFolders.Remove(ff);

      Helper.IsModifed = true;
    }

    public void AddDrives() {
      var drives = Environment.GetLogicalDrives();
      var drivesNames = new List<string>();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        var driveName = di.Name.Replace(Path.DirectorySeparatorChar.ToString(), string.Empty);
        drivesNames.Add(driveName);
        IconName driveImage;

        // set drive icon
        switch (di.DriveType) {
          case DriveType.CDRom:
            driveImage = IconName.Cd;
            break;
          case DriveType.Network:
            driveImage = IconName.Drive;
            break;
          case DriveType.NoRootDirectory:
          case DriveType.Unknown:
            driveImage = IconName.DriveError;
            break;
          default:
            driveImage = IconName.Drive;
            break;
        }

        // add Drive to the database and to the tree if not already exists
        if (!(Items.SingleOrDefault(x => x.Title.Equals(driveName)) is Folder item)) {
          item = new Folder(Helper.GetNextId(), driveName, null);
          AddRecord(item);
          Items.Add(item);
        }

        item.IsAccessible = di.IsReady;
        item.IconName = driveImage; 

        // add placeholder so the Drive can be expanded
        if (di.IsReady && item.Items.Count == 0)
          item.Items.Add(new BaseTreeViewItem { Title = "..." });
      }

      // remove not available drives
      for (var i = Items.Count - 1; i > -1; i--) {
        if (drivesNames.Any(x => x.Equals(Items[i].Title))) continue;
        Items.RemoveAt(i);
      }
    }

    public Folder GetByPath(string path) {
      if (path.Equals(string.Empty)) return null;

      var pathParts = path.Split(Path.DirectorySeparatorChar);
      var root = (BaseTreeViewItem) this;

      foreach (var pathPart in pathParts) {
        var folder = root.Items.SingleOrDefault(x => x.Title.Equals(pathPart));
        if (folder == null) return null;
        root = folder;
      }

      return root as Folder;
    }

    public void ExpandTo(Folder folder) {
      var parent = folder.Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = parent.Parent;
      }
    }

    public bool GetVisibleTreeIndexFor(ObservableCollection<BaseTreeViewItem> folders, Folder folder, ref int index) {
      foreach (var item in folders) {
        index++;
        if (item.Equals(folder)) return true;
        if (!item.IsExpanded) continue;
        if (GetVisibleTreeIndexFor(item.Items, folder, ref index)) return true;
      }
      return false;
    }
  }
}
