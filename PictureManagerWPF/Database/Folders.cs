using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using PictureManager.Dialogs;
using PictureManager.Properties;
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
        if (!string.IsNullOrEmpty(folder.Csv[2])) {
          folder.Parent = AllDic[int.Parse(folder.Csv[2])];
          folder.Parent.Items.Add(folder);
        }
        else { // drive
          Items.Add(folder);
        }

        // csv array is not needed any more
        folder.Csv = null;
      }

      AddDrives();
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic.Clear();
      Helper.LoadFromFile();
    }

    public void AddRecord(Folder record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public void DeleteRecord(Folder folder, bool fileSystem) {
      // delete folder, subfolders and mediaItems from file system
      if (fileSystem && Directory.Exists(folder.FullPath)) {
        AppCore.FileOperationDelete(new List<string> {folder.FullPath}, true, false);
      }

      // delete folder, subfolders and mediaItems from cache
      if (Directory.Exists(folder.FullPathCache)) {
        Directory.Delete(folder.FullPathCache, true);
      }

      // remove Folder from the Tree
      folder.Parent.Items.Remove(folder);

      // colapse parent if doesn't have any subfolders
      if (folder.Parent.Items.Count == 0)
        folder.Parent.IsExpanded = false;

      // get all folders recursive
      var folders = new List<BaseTreeViewItem>();
      folder.GetThisAndItemsRecursive(ref folders);

      foreach (var f in folders.OfType<Folder>()) {
        // remove Folder from DB
        All.Remove(f);
        AllDic.Remove(f.Id);

        // remove MediaItems
        foreach (var mi in f.MediaItems.ToList())
          App.Core.MediaItems.Delete(mi);

        // MediaItems should by empty from calling App.Core.MediaItems.Delete(mi)
        f.MediaItems.Clear();

        // remove Parent
        f.Parent = null;

        // clear subFolders
        f.Items.Clear();

        // remove FavoriteFolder
        var ff = App.Core.FavoriteFolders.All.SingleOrDefault(x => x.Folder.Id.Equals(f.Id));
        if (ff != null) App.Core.FavoriteFolders.Remove(ff);
      }

      // set Folders table as modifed
      Helper.IsModifed = true;
    }

    public void AddDrives() {
      All.Where(x => x.IsHidden).ToList().ForEach(x => x.IsHidden = false);

      var drives = Environment.GetLogicalDrives();
      var drivesNames = new List<string>();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        var driveName = di.Name.TrimEnd(Path.DirectorySeparatorChar);
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

        // if Viewer can't see this Drive set it as hidden and continue
        if (!Viewers.CanViewerSeeThisDirectory(App.Core.CurrentViewer ,item)) {
          item.IsHidden = true;
          continue;
        }

        item.IsAccessible = di.IsReady;
        item.IconName = driveImage; 

        // add placeholder so the Drive can be expanded
        if (di.IsReady && item.Items.Count == 0)
          item.Items.Add(new BaseTreeViewItem());
      }

      // set not available drives as hidden
      foreach (var item in Items) {
        if (drivesNames.Any(x => x.Equals(item.Title))) continue;
        ((Folder) item).IsHidden = true;
      }
    }

    public Folder GetByPath(string path, bool withReload = false) {
      if (string.IsNullOrEmpty(path)) return null;
      var pathParts = path.Split(Path.DirectorySeparatorChar);
      var drive = Items.SingleOrDefault(x => x.Title.Equals(pathParts[0])) as Folder;
      return pathParts.Length == 1 ? drive : drive?.GetByPath(path, withReload);
    }

    public bool GetVisibleTreeIndexFor(ObservableCollection<BaseTreeViewItem> folders, Folder folder, ref int index) {
      foreach (var item in folders.Cast<Folder>()) {
        index++;
        if (item.Id.Equals(folder.Id)) return true;
        if (!item.IsExpanded) continue;
        if (GetVisibleTreeIndexFor(item.Items, folder, ref index)) return true;
      }
      return false;
    }

    public Folder GetFromDic(string folderPath, ref Dictionary<string, Folder> dic) {
      if (dic.TryGetValue(folderPath, out var folder)) return folder;
      folder = GetByPath(folderPath, true);
      if (folder != null)
        dic.Add(folderPath, folder);

      return folder;
    }

    public void CopyMove(FileOperationMode mode, Folder srcFolder, Folder destFolder) {
      var fop = new FileOperationDialog { Owner = App.WMain };

      fop.Worker.DoWork += delegate (object sender, DoWorkEventArgs args) {
        var worker = (BackgroundWorker)sender;
        var skippedFiles = new HashSet<string>();
        var renamedFiles = new Dictionary<string, string>();

        // Copy/Move Files and Cache on file system
        CopyMoveFilesAndCache(mode, srcFolder.FullPath, Extensions.PathCombine(destFolder.FullPath, srcFolder.Title),
          fop, ref skippedFiles, ref renamedFiles, worker);

        // update objects with skipped and renamed files in mind
        switch (mode) {
          case FileOperationMode.Copy:
            srcFolder.CopyTo(destFolder, ref skippedFiles, ref renamedFiles);
            break;
          case FileOperationMode.Move:
            // Rename Renamed Files
            foreach (var renamedFile in renamedFiles) {
              var mi = srcFolder.GetMediaItemByPath(renamedFile.Key);
              // renamed files can contain files which are not in DB
              if (mi == null) continue;
              mi.FileName = renamedFile.Value;
            }

            srcFolder.MoveTo(destFolder, ref skippedFiles);
            break;
        } 
      };

      fop.PbProgress.IsIndeterminate = true;
      fop.Worker.RunWorkerAsync();
      fop.ShowDialog();
    }

    private void CopyMoveFilesAndCache(FileOperationMode mode, string srcDirPath, string destDirPath, Window owner,
      ref HashSet<string> skippedFiles, ref Dictionary<string, string> renamedFiles, BackgroundWorker worker) {

      try {
        Directory.CreateDirectory(destDirPath);
        var srcDirPathLength = srcDirPath.Length + 1;

        // run this function for each sub directory
        foreach (var dir in Directory.EnumerateDirectories(srcDirPath)) {
          CopyMoveFilesAndCache(mode, dir, Extensions.PathCombine(destDirPath, dir.Substring(srcDirPathLength)),
            owner, ref skippedFiles, ref renamedFiles, worker);
        }

        // get source and destination paths to Cache
        var srcDirPathCache = srcDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Settings.Default.CachePath);
        var destDirPathCache = destDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Settings.Default.CachePath);

        // for each file in the folder
        foreach (var srcFilePath in Directory.EnumerateFiles(srcDirPath)) {
          var srcFileName = srcFilePath.Substring(srcDirPathLength);
          var destFileName = srcFileName;
          var destFilePath = Extensions.PathCombine(destDirPath, destFileName);
          var srcFilePathCache = Extensions.PathCombine(srcDirPathCache, srcFileName);
          var destFilePathCache = Extensions.PathCombine(destDirPathCache, destFileName);

          worker.ReportProgress(0, new[] { srcDirPath, destDirPath, srcFileName });

          // if the file with the same name exists in the destination
          // show dialog with options to Rename, Replace or Skip the file
          if (File.Exists(destFilePath)) {
            var result = AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, owner, ref destFileName);

            switch (result) {
              case FileOperationCollisionDialog.CollisionResult.Rename: {
                  renamedFiles.Add(srcFilePath, destFileName);
                  destFilePath = Extensions.PathCombine(destDirPath, destFileName);
                  destFilePathCache = Extensions.PathCombine(destDirPathCache, destFileName);
                  break;
                }
              case FileOperationCollisionDialog.CollisionResult.Replace: {
                  File.Delete(destFilePath);
                  break;
                }
              case FileOperationCollisionDialog.CollisionResult.Skip: {
                  skippedFiles.Add(srcFilePath);
                  continue;
                }
            }
          }

          try {
            // Copy/Move Files
            switch (mode) {
              case FileOperationMode.Copy:
                File.Copy(srcFilePath, destFilePath, true);
                break;
              case FileOperationMode.Move:
                File.Move(srcFilePath, destFilePath);
                break;
            }

            // Copy/Move Cache
            if (File.Exists(srcFilePathCache)) {
              Directory.CreateDirectory(destDirPathCache);
              switch (mode) {
                case FileOperationMode.Copy:
                  File.Copy(srcFilePathCache, destFilePathCache, true);
                  break;
                case FileOperationMode.Move:
                  if (File.Exists(destFilePathCache))
                    File.Delete(destFilePathCache);
                  File.Move(srcFilePathCache, destFilePathCache);
                  break;
              }
            }
          }
          catch (Exception) {
            skippedFiles.Add(srcFilePath);
          }
        }

        // Delete empty directory
        if (mode == FileOperationMode.Move) {
          // if this is done on worker thread => directory is not deleted until worker is finished
          Application.Current.Dispatcher.Invoke(delegate {
            Extensions.DeleteDirectoryIfEmpty(srcDirPath);
            Extensions.DeleteDirectoryIfEmpty(srcDirPathCache);
          });
        }
      }
      catch (Exception ex) {
        AppCore.ShowErrorDialog(ex);
      }
    }
  }
}
