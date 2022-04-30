﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class FoldersM : TreeCategoryBase {
    private readonly Core _core;
    private readonly ViewersM _viewersM;

    public DataAdapter DataAdapter { get; set; }
    public List<FolderM> All { get; } = new();
    public Dictionary<int, FolderM> AllDic { get; set; }
    public event EventHandler<ObjectEventArgs<FolderM>> FolderDeletedEventHandler = delegate { };
    public static readonly FolderM FolderPlaceHolder = new(0, string.Empty, null);
    public Action<object, ITreeItem, bool, bool> OnDropAction { get; set; }

    public FoldersM(Core core, ViewersM viewersM) : base(Res.IconFolder, Category.Folders, "Folders") {
      _core = core;
      _viewersM = viewersM;

      CanMoveItem = true;
      CanCopyItem = true;
      IsExpanded = true;
    }

    public void HandleItemExpandedChanged(FolderM item) {
      item.UpdateIconName();

      if (item.IsExpanded)
        item.LoadSubFolders(false);
    }

    public override bool CanDrop(object src, ITreeItem dest) {
      switch (src) {
        case FolderM srcData: { // Folder
          if (dest is FolderM destData
            && !destData.HasThisParent(srcData)
            && !Equals(srcData, destData)
            && destData.IsAccessible
            && !Equals(srcData.Parent, destData))
            return true;

          break;
        }
        case string[] dragged: { // MediaItems
          if (_core.ThumbnailsGridsM.Current == null) break;

          var selected = _core.ThumbnailsGridsM.Current.FilteredItems
            .Where(x => x.IsSelected)
            .Select(p => p.FilePath)
            .OrderBy(p => p)
            .ToArray();

          if (selected.SequenceEqual(dragged.OrderBy(x => x))
              && dest is FolderM { IsAccessible: true })
            return true;

          break;
        }
      }

      return false;
    }

    public override void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
      OnDropAction(src, dest, aboveDest, copy);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var rootFolder = (FolderM)root;
      // create Folder
      Directory.CreateDirectory(IOExtensions.PathCombine(rootFolder.FullPath, name));
      var item = new FolderM(DataAdapter.GetNextId(), name, root) { IsAccessible = true };

      // add new Folder to the database
      All.Add(item);

      // add new Folder to the tree
      root.Items.SetInOrder(item, x => x.Name);

      // reload FolderKeywords
      if (rootFolder.IsFolderKeyword || rootFolder.FolderKeyword != null)
        _core.FolderKeywordsM.Load(All);

      return item;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      var folder = (FolderM)item;
      var parent = (FolderM)item.Parent;

      Directory.Move(folder.FullPath, IOExtensions.PathCombine(parent.FullPath, name));
      if (Directory.Exists(folder.FullPathCache))
        Directory.Move(folder.FullPathCache, IOExtensions.PathCombine(parent.FullPathCache, name));

      folder.Name = name;
      folder.Parent.Items.SetInOrder(folder, x => x.Name);
      DataAdapter.IsModified = true;

      // reload FolderKeywords
      if (folder.IsFolderKeyword || folder.FolderKeyword != null)
        _core.FolderKeywordsM.Load(All);
    }

    protected override void ModelItemDelete(ITreeItem item) {
      item.Parent.Items.Remove(item);

      // get all folders recursive
      var folders = new List<FolderM>();
      Tree.GetThisAndItemsRecursive(item, ref folders);

      foreach (var f in folders) {
        All.Remove(f);
        FolderDeletedEventHandler(this, new(f));

        f.Parent = null;
        f.Items.Clear();
        DataAdapter.IsModified = true;
      }

      _core.FolderKeywordsM.Load(All);

      // delete folder, sub folders and mediaItems from cache
      if (Directory.Exists(((FolderM)item).FullPathCache))
        Directory.Delete(((FolderM)item).FullPathCache, true);

      // delete folder, sub folders and mediaItems from file system
      // TODO it should be in Model
      // done in OnAfterItemDelete (FoldersTreeVM)
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) {
      // check if folder already exists
      if (Directory.Exists(IOExtensions.PathCombine(((FolderM)root).FullPath, name)))
        return "Folder already exists!";

      // check if is correct folder name
      if (Path.GetInvalidPathChars().Any(name.Contains))
        return "New folder's name contains incorrect character(s)!";

      return null;
    }

    public void ItemDelete(FolderM item) =>
      ModelItemDelete(item);

    public void AddDrives() {
      var drives = Environment.GetLogicalDrives();
      var drivesNames = new List<string>();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        var driveName = di.Name.TrimEnd(Path.DirectorySeparatorChar);
        drivesNames.Add(driveName);

        // add Drive to the database and to the tree if not already exists
        if (Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(driveName, StringComparison.CurrentCultureIgnoreCase)) is not { } item) {
          item = new DriveM(DataAdapter.GetNextId(), driveName, this);
          All.Add(item);
          Items.Add(item);
        }

        item.IsAccessible = di.IsReady;
        item.IconName = GetDriveIconName(di.DriveType);

        // add placeholder so the Drive can be expanded
        if (di.IsReady && item.Items.Count == 0)
          item.Items.Add(FolderPlaceHolder);
      }

      // set available drives
      foreach (var item in Items.Cast<FolderM>()) {
        item.IsAvailable = drivesNames.Any(x => x.Equals(item.Name, StringComparison.CurrentCultureIgnoreCase));
        item.IsHidden = !IsFolderVisible(item);
      }
    }

    public static string GetDriveIconName(DriveType type) =>
      type switch {
        DriveType.CDRom => Res.IconCd,
        DriveType.Network => Res.IconDrive,
        DriveType.NoRootDirectory or DriveType.Unknown => Res.IconDriveError,
        _ => Res.IconDrive,
      };

    public MediaItemM GetMediaItemByPath(string path) {
      var lioSep = path.LastIndexOf(Path.DirectorySeparatorChar);
      var folderPath = path[..lioSep];
      var fileName = path[(lioSep + 1)..];
      var folder = GetByPath(folderPath);
      return folder?.GetMediaItemByName(fileName);
    }

    private FolderM GetByPath(string path) {
      if (string.IsNullOrEmpty(path)) return null;
      var pathParts = path.Split(Path.DirectorySeparatorChar);
      var drive = Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(pathParts[0], StringComparison.OrdinalIgnoreCase));
      return pathParts.Length == 1 ? drive : drive?.GetByPath(path);
    }

    public bool IsFolderVisible(FolderM folder) =>
      Tree.GetTopParent(folder)?.IsAvailable == true && _viewersM.CanViewerSee(folder);

    public void CopyMove(FileOperationMode mode, FolderM srcFolder, FolderM destFolder, IProgress<object[]> progress,
      MediaItemsM.CollisionResolver collisionResolver, CancellationToken token) {
      var skippedFiles = new HashSet<string>();
      var renamedFiles = new Dictionary<string, string>();

      // Copy/Move Files and Cache on file system
      CopyMoveFilesAndCache(mode, srcFolder.FullPath, IOExtensions.PathCombine(destFolder.FullPath, srcFolder.Name),
        ref skippedFiles, ref renamedFiles, progress, collisionResolver, token);

      // update objects with skipped and renamed files in mind
      switch (mode) {
        case FileOperationMode.Copy: {
          Core.RunOnUiThread(() => CopyFolder(srcFolder, destFolder, ref skippedFiles, ref renamedFiles));
          break;
        }
        case FileOperationMode.Move: {
          // Rename Renamed Files
          foreach (var (oldFilePath, newFileName) in renamedFiles) {
            var fileName = oldFilePath[(oldFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
            var mi = srcFolder.GetMediaItemByName(fileName);
            // renamed files can contain files which are not in DB
            if (mi == null) continue;
            mi.FileName = newFileName;
          }

          Core.RunOnUiThread(() => MoveFolder(srcFolder, destFolder, ref skippedFiles));
          break;
        }
      }

      _core.FolderKeywordsM.Load(All);
    }

    private static void CopyMoveFilesAndCache(FileOperationMode mode, string srcDirPath, string destDirPath,
      ref HashSet<string> skippedFiles, ref Dictionary<string, string> renamedFiles, IProgress<object[]> progress,
      MediaItemsM.CollisionResolver collisionResolver, CancellationToken token) {

      Directory.CreateDirectory(destDirPath);
      var srcDirPathLength = srcDirPath.Length + 1;

      // run this function for each sub directory
      foreach (var dir in Directory.EnumerateDirectories(srcDirPath)) {
        CopyMoveFilesAndCache(mode, dir, IOExtensions.PathCombine(destDirPath, dir[srcDirPathLength..]),
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
        var destFilePath = IOExtensions.PathCombine(destDirPath, destFileName);
        var srcFilePathCache = IOExtensions.PathCombine(srcDirPathCache, srcFileName);
        var destFilePathCache = IOExtensions.PathCombine(destDirPathCache, destFileName);

        progress.Report(new object[] { 0, srcDirPath, destDirPath, srcFileName });

        // if the file with the same name exists in the destination
        // show dialog with options to Rename, Replace or Skip the file
        if (File.Exists(destFilePath)) {
          var result = collisionResolver.Invoke(srcFilePath, destFilePath, ref destFileName);

          switch (result) {
            case CollisionResult.Rename: {
              renamedFiles.Add(srcFilePath, destFileName);
              destFilePath = IOExtensions.PathCombine(destDirPath, destFileName);
              destFilePathCache = IOExtensions.PathCombine(destDirPathCache, destFileName);
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
        Core.RunOnUiThread(() => {
          IOExtensions.DeleteDirectoryIfEmpty(srcDirPath);
          IOExtensions.DeleteDirectoryIfEmpty(srcDirPathCache);
        });
      }
    }

    private void CopyFolder(FolderM src, FolderM dest, ref HashSet<string> skipped, ref Dictionary<string, string> renamed) {
      // reload destFolder so that new folder is added
      dest.LoadSubFolders(false);

      // get target folder
      var targetFolder = dest.GetByPath(src.Name);
      if (targetFolder == null) return; // if folder doesn't exists => nothing was copied

      // Copy all MediaItems to target folder
      foreach (var mi in src.MediaItems) {
        var filePath = mi.FilePath;
        var fileName = mi.FileName;

        // skip if this file was skipped
        if (skipped.Remove(filePath)) continue;

        // change the file name if the file was renamed
        if (renamed.TryGetValue(filePath, out var newFileName)) {
          fileName = newFileName;
          renamed.Remove(filePath);
        }

        _core.MediaItemsM.CopyTo(mi, targetFolder, fileName);
      }

      // Copy all subFolders
      foreach (var subFolder in src.Items.OfType<FolderM>())
        CopyFolder(subFolder, targetFolder, ref skipped, ref renamed);

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (src.Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(FolderPlaceHolder);
    }

    private void MoveFolder(FolderM src, FolderM dest, ref HashSet<string> skipped) {
      // get target folder without reload!
      var targetFolder = dest.Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(src.Name, StringComparison.OrdinalIgnoreCase));
      var srcExists = Directory.Exists(src.FullPath);
      var deleteSrc = !srcExists && targetFolder != null;

      // if nothing was skipped and folder with the same name doesn't exist in destination
      if (!srcExists && targetFolder == null) {
        src.Parent.Items.Remove(src);
        src.Parent = dest;
        DataAdapter.IsModified = true;

        // add folder to the tree if destination is empty
        if (dest.Items.Count == 1 && FolderPlaceHolder.Equals(dest.Items[0])) {
          dest.Items.Clear();
          dest.Items.Add(src);
          return;
        }

        // insert folder to the tree in sort order
        dest.Items.SetInOrder(src, x => ((FolderM)x).Name);

        return;
      }

      if (targetFolder == null) {
        // reload destFolder so that new folder is added
        dest.LoadSubFolders(false);
        targetFolder = dest.GetByPath(src.Name);
      }
      if (targetFolder == null) throw new DirectoryNotFoundException();

      // Move all MediaItems to target folder
      foreach (var mi in src.MediaItems.ToArray()) {
        // skip if this file was skipped
        if (skipped.Remove(mi.FilePath)) continue;

        _core.MediaItemsM.MoveTo(mi, targetFolder, mi.FileName);
      }

      // Move all subFolders
      foreach (var subFolder in src.Items.OfType<FolderM>().ToArray())
        MoveFolder(subFolder, targetFolder, ref skipped);

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (src.Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(FolderPlaceHolder);

      // delete if src folder was moved completely and the target folder was already in DB
      if (deleteSrc)
        ModelItemDelete(src);
    }

    public static List<FolderM> GetFolders(List<FolderM> roots, bool recursive) {
      if (!recursive) return roots;

      var output = new List<FolderM>();
      foreach (var root in roots) {
        root.LoadSubFolders(true);
        Tree.GetThisAndItemsRecursive(root, ref output);
      }

      return output.ToList();
    }

    public void SetAsFolderKeyword(FolderM folder) {
      folder.IsFolderKeyword = true;
      DataAdapter.IsModified = true;
      _core.FolderKeywordsM.Load(All);
    }
  }
}
