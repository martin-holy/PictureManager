using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.ViewModel {
  public class Folders : BaseTreeViewItem {

    public Folders() {
      Title = "Folders";
      IconName = "appbar_folder";
    }

    public void AddDrives() {
      Items.Clear();
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
          IconName = driveImage,
          IsAccessible = di.IsReady
        };

        if (di.IsReady)
          item.Items.Add(new Folder {Title = "..."});

        Items.Add(item);
      }
    }

    public Folder ExpandTo(string fullPath) {
      var items = Items;
      while (true) {
        var folder = items.Cast<Folder>().FirstOrDefault(f => fullPath.StartsWith(f.FullPath, StringComparison.OrdinalIgnoreCase));
        if (folder == null) return null;
        if (folder.Items.Count != 0) folder.IsExpanded = true;
        if (fullPath.Equals(folder.FullPath, StringComparison.OrdinalIgnoreCase)) return folder;
        items = folder.Items;
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
