using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Folders : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public Folders() : base(Category.Folders) {
      Title = "Folders";
      IconName = IconName.Folder;
    }

    public void NewFromCsv(string csv) {
      // ID|Name|Parent|IsFolderKeyword|SubFolders|MediaItems
      var props = csv.Split('|');
      if (props.Length != 6) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Folder(id, props[1], null) {Csv = props, IsFolderKeyword = props[3] == "1"});
    }

    public void LinkReferences(SimpleDB sdb) {
      // ID|Name|Parent|IsFolderKeyword|SubFolders|MediaItems
      foreach (var item in Records) {
        var folder = (Folder)item.Value;

        // reference to parent
        if (folder.Csv[2] != string.Empty)
          folder.Parent = (Folder)Records[int.Parse(folder.Csv[2])];

        // reference to subfolders
        if (folder.Csv[4] != string.Empty)
          foreach (var folderId in folder.Csv[4].Split(','))
            folder.Items.Add((Folder)Records[int.Parse(folderId)]);

        // reference to mediaItems
        if (folder.Csv[5] != string.Empty)
          foreach (var miId in folder.Csv[5].Split(','))
            folder.MediaItems.Add((BaseMediaItem)ACore.MediaItems.Records[int.Parse(miId)]);

        // csv array is not needed any more
        folder.Csv = null;
      }

      foreach (var topFolder in Records.Values.Cast<Folder>().Where(x => x.Parent == null)) {
        Items.Add(topFolder);
      }

      AddDrives();
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
          item = new Folder(ACore.Folders.Helper.GetNextId(), driveName, null);
          ACore.Folders.Helper.AddRecord(item);
          Items.Add(item);
        }

        item.IsAccessible = di.IsReady;
        item.IconName = driveImage; 

        // add placeholder so the Drive can be expanded
        if (di.IsReady && item.Items.Count == 0)
          item.Items.Add(new BaseTreeViewItem { Title = "..." });
      }

      // remove not available drives
      foreach (var item in Items) {
        if (drivesNames.Any(x => x.Equals(item.Title))) continue;
        Items.Remove(item);
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

    public Folder ExpandTo(int id) {
      if (!ACore.Folders.Records.TryGetValue(id, out var folder)) return null;
      var parent = ((Folder) folder).Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = parent.Parent;
      }

      return (Folder) folder;
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
