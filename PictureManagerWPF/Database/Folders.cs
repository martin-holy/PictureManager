using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Folders : VM.BaseCategoryItem, ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public Folders() : base(Category.Folders) {
      Title = "Folders";
      IconName = IconName.Folder;
    }

    public void NewFromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Folder(id, props[1], null) {Csv = props, IsFolderKeyword = props[3] == "1"});
    }

    public void LinkReferences(SimpleDB sdb) {
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
            folder.MediaItems.Add((BaseMediaItem)ACore.NewMediaItems.Records[int.Parse(miId)]);

        // csv array is not needed any more
        folder.Csv = null;
      }
    }

    public void AddDrives() {
      var drives = Environment.GetLogicalDrives();
      var drivesNames = new List<string>();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        drivesNames.Add(di.Name);
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

        // check if the Drive already exists in the tree
        var existingDir = Items.SingleOrDefault(x => x.Title.Equals(di.Name));

        // continue if the Drive already exists in the tree
        if (existingDir != null) continue;

        // add new Drive to the database and to the tree
        var item = new Folder(ACore.Sdb.Table<Folders>().GetNextId(), di.Name, null) {
          IsAccessible = true,
          IconName = driveImage
        };
        ACore.Sdb.Table<Folders>().AddRecord(item);
        Items.Add(item);

        // add placeholder so the Drive can be expanded
        if (di.IsReady)
          item.Items.Add(new VM.BaseTreeViewItem { Title = "..." });
      }

      // remove not available drives
      foreach (var item in Items) {
        if (drivesNames.Any(x => x.Equals(item.Title))) continue;
        Items.Remove(item);
      }
    }

    public Folder ExpandTo(int id) {
      if (!ACore.Sdb.Table<Folders>().Table.Records.TryGetValue(id, out var folder)) return null;
      var parent = ((Folder) folder).Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = parent.Parent;
      }

      return (Folder) folder;
    }

    public bool GetVisibleTreeIndexFor(ObservableCollection<VM.BaseTreeViewItem> folders, Folder folder, ref int index) {
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
