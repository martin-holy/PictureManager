using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace PictureManager.Domain.Models {
  public sealed class Folders : BaseCatTreeViewCategory, ITable {
    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Folder> AllDic { get; set; }

    public Folders(Core core) : base(Category.Folders) {
      _core = core;
      DataAdapter = new FoldersDataAdapter(core, this);
      Title = "Folders";
      IconName = IconName.Folder;
      CanHaveSubItems = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
      CanCopyItem = true;
    }

    public void AddDrives() {
      All.Cast<Folder>().Where(x => x.IsHidden).ToList().ForEach(x => x.IsHidden = false);

      var drives = Environment.GetLogicalDrives();
      var drivesNames = new List<string>();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        var driveImage = GetDriveIconName(di.DriveType);
        var driveName = di.Name.TrimEnd(Path.DirectorySeparatorChar);
        drivesNames.Add(driveName);

        // add Drive to the database and to the tree if not already exists
        if (Items.SingleOrDefault(x => x.Title.Equals(driveName, StringComparison.Ordinal)) is not Folder item) {
          item = new Folder(DataAdapter.GetNextId(), driveName, this);
          All.Add(item);
          Items.Add(item);
        }

        item.IsAccessible = di.IsReady;
        item.IconName = driveImage;
        item.IsExpanded = false;

        // if Viewer can't see this Drive set it as hidden and continue
        if (!_core.CanViewerSeeThisFolder(item)) {
          item.IsHidden = true;
          continue;
        }

        // add placeholder so the Drive can be expanded
        if (di.IsReady && item.Items.Count == 0)
          item.Items.Add(new CatTreeViewItem());
      }

      // set not available drives as hidden
      foreach (var item in Items) {
        if (drivesNames.Any(x => x.Equals(item.Title))) continue;
        ((Folder)item).IsHidden = true;
      }
    }

    public static IconName GetDriveIconName(DriveType type) =>
      type switch {
        DriveType.CDRom => IconName.Cd,
        DriveType.Network => IconName.Drive,
        DriveType.NoRootDirectory or DriveType.Unknown => IconName.DriveError,
        _ => IconName.Drive,
      };

    public Folder GetByPath(string path) {
      if (string.IsNullOrEmpty(path)) return null;
      var pathParts = path.Split(Path.DirectorySeparatorChar);
      var drive = Items.SingleOrDefault(x => x.Title.Equals(pathParts[0], StringComparison.CurrentCultureIgnoreCase)) as Folder;
      return pathParts.Length == 1 ? drive : drive?.GetByPath(path);
    }

    public MediaItem GetMediaItemByPath(string path) =>
      GetByPath(path[..path.LastIndexOf(Path.DirectorySeparatorChar)])?.GetMediaItemByPath(path);

    public bool GetVisibleTreeIndexFor(ObservableCollection<ICatTreeViewItem> folders, Folder folder, ref int index) {
      foreach (var item in folders.Cast<Folder>()) {
        index++;
        if (item.Id.Equals(folder.Id)) return true;
        if (!item.IsExpanded) continue;
        if (GetVisibleTreeIndexFor(item.Items, folder, ref index)) return true;
      }
      return false;
    }

    public void SortInDB(List<IRecord> sorted) {
      for (int i = 0; i < sorted.Count; i++)
        All.Move(sorted[i], i);
      DataAdapter.IsModified = true;
    }

    public static void CopyMove(FileOperationMode mode, Folder srcFolder, Folder destFolder, IProgress<object[]> progress,
      MediaItems.CollisionResolver collisionResolver, CancellationToken token) {
      var skippedFiles = new HashSet<string>();
      var renamedFiles = new Dictionary<string, string>();

      // Copy/Move Files and Cache on file system
      CopyMoveFilesAndCache(mode, srcFolder.FullPath, Extensions.PathCombine(destFolder.FullPath, srcFolder.Title),
        ref skippedFiles, ref renamedFiles, progress, collisionResolver, token);

      // update objects with skipped and renamed files in mind
      switch (mode) {
        case FileOperationMode.Copy: {
          Core.Instance.RunOnUiThread(() => srcFolder.CopyTo(destFolder, ref skippedFiles, ref renamedFiles));
          break;
        }
        case FileOperationMode.Move: {
          // Rename Renamed Files
          foreach (var renamedFile in renamedFiles) {
            var mi = srcFolder.GetMediaItemByPath(renamedFile.Key);
            // renamed files can contain files which are not in DB
            if (mi == null) continue;
            mi.FileName = renamedFile.Value;
          }

          Core.Instance.RunOnUiThread(() => srcFolder.MoveTo(destFolder, ref skippedFiles));
          break;
        }
      }
    }

    private static void CopyMoveFilesAndCache(FileOperationMode mode, string srcDirPath, string destDirPath,
      ref HashSet<string> skippedFiles, ref Dictionary<string, string> renamedFiles, IProgress<object[]> progress,
      MediaItems.CollisionResolver collisionResolver, CancellationToken token) {

      Directory.CreateDirectory(destDirPath);
      var srcDirPathLength = srcDirPath.Length + 1;

      // run this function for each sub directory
      foreach (var dir in Directory.EnumerateDirectories(srcDirPath)) {
        CopyMoveFilesAndCache(mode, dir, Extensions.PathCombine(destDirPath, dir[srcDirPathLength..]),
          ref skippedFiles, ref renamedFiles, progress, collisionResolver, token);
      }

      // get source and destination paths to Cache
      var srcDirPathCache = srcDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath);
      var destDirPathCache = destDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath);

      // for each file in the folder
      foreach (var srcFilePath in Directory.EnumerateFiles(srcDirPath)) {
        if (token.IsCancellationRequested) {
          //INFO: I am not sure if I need to add all files to skippedFiles on cancel operation
          skippedFiles.Add(srcFilePath);
          continue;
        }

        var srcFileName = srcFilePath[srcDirPathLength..];
        var destFileName = srcFileName;
        var destFilePath = Extensions.PathCombine(destDirPath, destFileName);
        var srcFilePathCache = Extensions.PathCombine(srcDirPathCache, srcFileName);
        var destFilePathCache = Extensions.PathCombine(destDirPathCache, destFileName);

        progress.Report(new object[] { 0, srcDirPath, destDirPath, srcFileName });

        // if the file with the same name exists in the destination
        // show dialog with options to Rename, Replace or Skip the file
        if (File.Exists(destFilePath)) {
          var result = collisionResolver.Invoke(srcFilePath, destFilePath, ref destFileName);

          switch (result) {
            case CollisionResult.Rename: {
              renamedFiles.Add(srcFilePath, destFileName);
              destFilePath = Extensions.PathCombine(destDirPath, destFileName);
              destFilePathCache = Extensions.PathCombine(destDirPathCache, destFileName);
              break;
            }
            case CollisionResult.Replace: {
              File.Delete(destFilePath);
              break;
            }
            case CollisionResult.Skip: {
              skippedFiles.Add(srcFilePath);
              continue;
            }
          }
        }

        try {
          // Copy/Move Files
          switch (mode) {
            case FileOperationMode.Copy: {
              File.Copy(srcFilePath, destFilePath, true);
              break;
            }
            case FileOperationMode.Move: {
              File.Move(srcFilePath, destFilePath);
              break;
            }
          }

          // Copy/Move Cache
          if (File.Exists(srcFilePathCache)) {
            Directory.CreateDirectory(destDirPathCache);
            switch (mode) {
              case FileOperationMode.Copy: {
                File.Copy(srcFilePathCache, destFilePathCache, true);
                break;
              }
              case FileOperationMode.Move: {
                if (File.Exists(destFilePathCache))
                  File.Delete(destFilePathCache);
                File.Move(srcFilePathCache, destFilePathCache);
                break;
              }
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
        Core.Instance.RunOnUiThread(() => {
          Extensions.DeleteDirectoryIfEmpty(srcDirPath);
          Extensions.DeleteDirectoryIfEmpty(srcDirPathCache);
        });
      }
    }

    public override bool CanDrop(object src, ICatTreeViewItem dest) {
      switch (src) {
        case Folder srcData: { // Folder
          if (dest is Folder destData && !destData.HasThisParent(srcData) && !Equals(srcData, destData) &&
              destData.IsAccessible && !Equals((Folder)srcData.Parent, destData)) return true;

          break;
        }
        case string[] dragged: { // MediaItems
          if (_core.MediaItems.ThumbsGrid == null) break;

          var selected = _core.MediaItems.ThumbsGrid.FilteredItems
            .Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

          if (selected.SequenceEqual(dragged.OrderBy(x => x)) && dest is Folder destData && destData.IsAccessible) return true;

          break;
        }
      }

      return false;
    }

    public override void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      // handled in OnAfterOnDrop (TreeViewCategories)
    }

    public override bool CanCreateItem(ICatTreeViewItem item) => item is Folder;

    public override bool CanRenameItem(ICatTreeViewItem item) => item is Folder && !(item.Parent is ICatTreeViewCategory);

    public override bool CanDeleteItem(ICatTreeViewItem item) => item is Folder && !(item.Parent is ICatTreeViewCategory);

    public override bool CanSort(ICatTreeViewItem root) => false;

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) {
      // check if folder already exists
      if (Directory.Exists(Extensions.PathCombine(((Folder)root).FullPath, name)))
        return "Folder already exists!";

      // check if is correct folder name
      if (Path.GetInvalidPathChars().Any(name.Contains))
        return "New folder's name contains incorrect character(s)!";

      return null;
    }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      root.IsExpanded = true;

      // create Folder
      Directory.CreateDirectory(Extensions.PathCombine(((Folder)root).FullPath, name));
      var item = new Folder(DataAdapter.GetNextId(), name, root) { IsAccessible = true };

      // add new Folder to the database
      All.Add(item);

      // add new Folder to the tree
      CatTreeViewUtils.SetItemInPlace(root, item);

      DataAdapter.IsModified = true;

      // reload FolderKeywords
      if (((Folder)root).IsFolderKeyword || ((Folder)root).FolderKeyword != null)
        _core.FolderKeywords.Load();

      return item;
    }

    public override void ItemRename(ICatTreeViewItem item, string name) {
      var parent = (Folder)item.Parent;
      var self = (Folder)item;

      Directory.Move(self.FullPath, Extensions.PathCombine(parent.FullPath, name));
      if (Directory.Exists(self.FullPathCache))
        Directory.Move(self.FullPathCache, Extensions.PathCombine(parent.FullPathCache, name));

      item.Title = name;

      CatTreeViewUtils.SetItemInPlace(item.Parent, item);
      DataAdapter.IsModified = true;

      // reload FolderKeywords
      if (self.IsFolderKeyword || self.FolderKeyword != null)
        _core.FolderKeywords.Load();
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      // remove Folder from the Tree
      item.Parent.Items.Remove(item);

      // collapse parent if doesn't have any subfolders
      if (item.Parent.Items.Count == 0)
        item.Parent.IsExpanded = false;

      // get all folders recursive
      var folders = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndItemsRecursive(item, ref folders);

      foreach (var f in folders.OfType<Folder>()) {
        // remove Folder from DB
        All.Remove(f);

        // remove MediaItems
        foreach (var mi in f.MediaItems.ToList())
          _core.MediaItems.Delete(mi);

        // MediaItems should by empty from calling App.Core.MediaItems.Delete(mi)
        f.MediaItems.Clear();

        // remove Parent
        f.Parent = null;

        // clear subFolders
        f.Items.Clear();

        // remove FavoriteFolder
        _core.FavoriteFolders.ItemDelete(
          _core.FavoriteFolders.All.Cast<FavoriteFolder>().SingleOrDefault(x => x.Folder.Id.Equals(f.Id)));

        // set Folders table as modified
        DataAdapter.IsModified = true;
      }

      _core.FolderKeywords.Load();

      // delete folder, subfolders and mediaItems from cache
      if (Directory.Exists(((Folder)item).FullPathCache))
        Directory.Delete(((Folder)item).FullPathCache, true);

      // delete folder, subfolders and mediaItems from file system
      // done in OnAfterItemDelete (TreeViewItemsEvents)
    }
  }
}
