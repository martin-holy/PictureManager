using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.Services;

public sealed class FolderS(FolderR r) {
  public static readonly FolderM FolderPlaceHolder = new(0, string.Empty, null);
  public event EventHandler<ObjectEventArgs<FolderM>> ItemCopiedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<FolderM>> ItemMovedEvent = delegate { };

  public static void DeleteFromDisk(FolderM item) {
    if (Directory.Exists(item.FullPathCache))
      Directory.Delete(item.FullPathCache, true);

    if (Directory.Exists(item.FullPath))
      Core.FileOperationDelete([item.FullPath], true, false);
  }

  public static string GetDriveIcon(DriveType type) =>
    type switch {
      DriveType.CDRom => Res.IconCd,
      DriveType.Network => Res.IconDrive,
      DriveType.NoRootDirectory or DriveType.Unknown => Res.IconDriveError,
      _ => Res.IconDrive,
    };

  public void CopyMove(FileOperationMode mode, FolderM srcFolder, FolderM destFolder) {
    var fop = new FileOperationDialogM(mode, true);
    fop.RunTask = Task.Run(() => {
      fop.LoadCts = new();
      var token = fop.LoadCts.Token;

      try {
        CopyMove(mode, srcFolder, destFolder, fop.Progress, token);
      }
      catch (Exception ex) {
        Tasks.RunOnUiThread(() => Dialog.Show(new ErrorDialogM(ex)));
      }
    }).ContinueWith(_ => Tasks.RunOnUiThread(() => fop.Result = 1));

    Dialog.Show(fop);

    switch (mode) {
      case FileOperationMode.Copy:
        ItemCopiedEvent(this, new(srcFolder));
        break;
      case FileOperationMode.Move:
        ItemMovedEvent(this, new(srcFolder));
        break;
    }
  }

  private void CopyMove(FileOperationMode mode, FolderM srcFolder, FolderM destFolder, IProgress<object[]> progress,
    CancellationToken token) {
    var skippedFiles = new HashSet<string>();
    var renamedFiles = new Dictionary<string, string>();

    // Copy/Move Files and Cache on file system
    CopyMoveFilesAndCache(mode, srcFolder.FullPath, IOExtensions.PathCombine(destFolder.FullPath, srcFolder.Name),
      ref skippedFiles, ref renamedFiles, progress, token);

    // update objects with skipped and renamed files in mind
    switch (mode) {
      case FileOperationMode.Copy: {
        Tasks.RunOnUiThread(() => CopyFolder(srcFolder, destFolder, ref skippedFiles, ref renamedFiles));
        break;
      }
      case FileOperationMode.Move: {
        // Rename Renamed Files
        foreach (var (oldFilePath, newFileName) in renamedFiles) {
          var fileName = oldFilePath[(oldFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
          var mi = srcFolder.MediaItems.GetByFileName(fileName);
          // renamed files can contain files which are not in DB
          // TODO remove this
          if (mi == null) continue;
          mi.FileName = newFileName;
        }

        Tasks.RunOnUiThread(() => MoveFolder(srcFolder, destFolder, ref skippedFiles));
        break;
      }
    }
  }

  private static void CopyMoveFilesAndCache(FileOperationMode mode, string srcDirPath, string destDirPath,
    ref HashSet<string> skippedFiles, ref Dictionary<string, string> renamedFiles, IProgress<object[]> progress,
    CancellationToken token) {

    Directory.CreateDirectory(destDirPath);
    var srcDirPathLength = srcDirPath.Length + 1;

    // run this function for each sub directory
    foreach (var dir in Directory.EnumerateDirectories(srcDirPath))
      CopyMoveFilesAndCache(mode, dir, IOExtensions.PathCombine(destDirPath, dir[srcDirPathLength..]),
        ref skippedFiles, ref renamedFiles, progress, token);

    // get source and destination paths to Cache
    var srcDirPathCache = srcDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.CachePath);
    var destDirPathCache = destDirPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.CachePath);

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
        var result = FileOperationCollisionDialogM.Open(srcFilePath, destFilePath, ref destFileName);

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
    if (mode == FileOperationMode
          .Move) // if this is done on worker thread => directory is not deleted until worker is finished
      Tasks.RunOnUiThread(() => {
        IOExtensions.DeleteDirectoryIfEmpty(srcDirPath);
        IOExtensions.DeleteDirectoryIfEmpty(srcDirPathCache);
      });
  }

  private void CopyFolder(FolderM src, FolderM dest, ref HashSet<string> skipped,
    ref Dictionary<string, string> renamed) {
    // reload destFolder so that new folder is added
    dest.LoadSubFolders(false);

    // get target folder
    var targetFolder = Tree.GetByPath(dest, src.Name, Path.DirectorySeparatorChar) as FolderM;
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

      Core.R.MediaItem.ItemCopy(mi, targetFolder, fileName);
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
    var targetFolder = dest.GetByName(src.Name);
    var srcExists = Directory.Exists(src.FullPath);
    var deleteSrc = !srcExists && targetFolder != null;

    // if nothing was skipped and folder with the same name doesn't exist in destination
    if (!srcExists && targetFolder == null) {
      src.Parent.Items.Remove(src);
      src.Parent = dest;
      r.IsModified = true;

      // add folder to the tree if destination is empty
      if (dest.Items.Count == 1 && ReferenceEquals(FolderPlaceHolder, dest.Items[0])) {
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
      targetFolder = Tree.GetByPath(dest, src.Name, Path.DirectorySeparatorChar) as FolderM;
    }

    if (targetFolder == null) throw new DirectoryNotFoundException();

    // Move all MediaItems to target folder
    foreach (var mi in src.MediaItems.ToArray()) {
      // skip if this file was skipped
      if (skipped.Remove(mi.FilePath)) continue;

      Core.R.MediaItem.ItemMove(mi, targetFolder, mi.FileName);
    }

    // Move all subFolders
    foreach (var subFolder in src.Items.OfType<FolderM>().ToArray())
      MoveFolder(subFolder, targetFolder, ref skipped);

    // if srcFolder have subFolders and targetFolder not => add place holder
    if (src.Items.Count > 0 && targetFolder.Items.Count == 0)
      targetFolder.Items.Add(FolderPlaceHolder);

    // delete if src folder was moved completely and the target folder was already in DB
    if (deleteSrc)
      r.TreeItemDelete(src);
  }

  public FolderM[] GetFolders(ITreeItem item, bool recursive) {
    var roots = (item as FolderKeywordM)?.Folders?.ToArray() ?? new[] { (FolderM)item };

    if (!recursive) return roots;

    foreach (var root in roots)
      root.LoadSubFolders(true);

    return roots.SelectMany(x => x.Flatten()).Where(Core.S.Viewer.CanViewerSee).ToArray();
  }
}