using System;
using System.IO;
using System.Linq;

namespace PictureManager.Data {
  public class Folder: DataBase {
    public Folder Parent { get; set; }

    public override bool IsExpanded {
      get { return base.IsExpanded; }
      set {
        base.IsExpanded = value;
        if (value) GetSubFolders(false);
        if (Parent != null)
          ImageName = IsExpanded ? "appbar_folder_open" : "appbar_folder";
      }
    }

    public void Rename(string name) {
      //TODO presun slozky, presun cache, update vsech FullPath, update DB
      //AppStuff.FolderMoveWithCache(path, newPath)



      Title = name;
    }

    public void GetSubFolders(bool refresh) {
      if (!refresh) {
        if (Items.Count <= 0) return;
        if (((Folder)Items[0]).Title != @"...") return;
      }
      Items.Clear();

      foreach (string dir in Directory.GetDirectories(FullPath).OrderBy(x => x)) {
        DirectoryInfo di = new DirectoryInfo(dir);
        Folder item = new Folder {Title = di.Name, FullPath = dir, ImageName = "appbar_folder", Parent = this};
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new Folder {Title = "..."});
        } catch (UnauthorizedAccessException) {
          item.ImageName = "appbar_folder_lock";
          item.Accessible = false;
        } finally {
          Items.Add(item);
        }
      }
    }

    public void AddDrives() {
      string[] drives = Environment.GetLogicalDrives();

      foreach (string drive in drives) {
        DriveInfo di = new DriveInfo(drive);
        string driveImage;

        switch (di.DriveType) {
          case DriveType.CDRom:
            driveImage = "appbar_cd";
            break;
          case DriveType.Network:
            driveImage = "appbar_drive";
            break;
          case DriveType.NoRootDirectory:
          case DriveType.Unknown:
            driveImage = "appbar_drive_error";
            break;
          default:
            driveImage = "appbar_drive";
            break;
        }
        Folder item = new Folder {
          Title = $"{(di.IsReady ? di.VolumeLabel : di.DriveType.ToString())} ({di.Name})",
          FullPath = drive,
          ImageName = driveImage,
          Accessible = di.IsReady};

        if (di.IsReady)
          item.Items.Add(new Folder {Title = "..."});

        Items.Add(item);
      }
    }
  }
}
