using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Utils;

// BUG there is new fileNameCache for VideoItems on Copy mode as well like in segments
public sealed class CopyMoveU(FileOperationMode mode, CoreR coreR) {
  private readonly FileOperationDialogM _dlg = new($"File Operation ({mode})");
  private readonly Dictionary<MediaItemM, string> _renamed = new();
  private readonly HashSet<MediaItemM> _replaced = [];
  private readonly List<(SegmentM, SegmentM)> _segments = [];

  public HashSet<MediaItemM> Skipped = [];

  public T Do<T>(Task<T> work) {
    _dlg.SetWorkTask(work);
    Dialog.Show(_dlg);
    return work.Result;
  }

  public static void CopyMoveFolder(FolderM src, FolderM dest, FileOperationMode mode) {
    try {
      Core.R.IsCopyMoveInProgress = true;
      var cm = new CopyMoveU(mode, Core.R);
      if (!cm.Do(cm.CopyMoveFolder(src, dest))) return;

      Core.R.FolderKeyword.Reload();
      if (mode == FileOperationMode.Move)
        Core.VM.MainWindow.StatusBar.UpdateFilePath();
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
    finally {
      Core.R.IsCopyMoveInProgress = false;
    }
  }

  public static void CopyMoveMediaItems(RealMediaItemM[] items, FolderM dest, FileOperationMode mode) {
    try {
      Core.R.IsCopyMoveInProgress = true;
      var cm = new CopyMoveU(mode, Core.R);
      if (!cm.Do(cm.CopyMoveMediaItems(items, dest))) return;

      if (mode == FileOperationMode.Move) {
        var mis = items.Except(cm.Skipped).ToList();
        Core.VM.MediaItem.Current = null;
        Core.VM.MediaItem.Views.Current.Remove(mis, true);
      }
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
    finally {
      Core.R.IsCopyMoveInProgress = false;
    }
  }

  public async Task<bool> CopyMoveFolder(FolderM src, FolderM dest) {
    _dlg.IsIndeterminate = true;
    await Task.Run(() => CopyMoveFolderRecursive(src, dest));
    CopyMoveFolderInDBRecursive(src, dest);
    CopySegmentsCacheOnDrive();
    return true;
  }

  public async Task<bool> CopyMoveMediaItems(RealMediaItemM[] items, FolderM dest) {
    _dlg.IsIndeterminate = false;
    await Task.Run(() => CopyMoveMediaItemsFiles(items, dest));
    CopyMoveMediaItemsInDB(items, dest);
    CopySegmentsCacheOnDrive();
    return true;
  }

  private Task CopyMoveFolderRecursive(FolderM src, FolderM dest) {
    // create target folder and reload sub folders to update DB
    Directory.CreateDirectory(IOExtensions.PathCombine(dest.FullPath, src.Name));
    Tasks.RunOnUiThread(() => {
      src.LoadSubFolders(false);
      dest.LoadSubFolders(false);
    }).Wait();

    var srcPath = src.FullPath;
    var srcPathCache = src.FullPathCache;
    var srcPathLength = srcPath.Length + 1;
    var target = dest.GetByName(src.Name);
    var targetPath = target.FullPath;
    var targetPathCache = target.FullPathCache;

    // for each file in the folder (all files *.*)
    foreach (var srcFilePath in Directory.EnumerateFiles(srcPath)) {
      var srcFileName = srcFilePath[srcPathLength..];
      var mi = src.MediaItems.GetByFileName(srcFileName);

      if (_dlg.WorkCts.Token.IsCancellationRequested) {
        if (mi != null) Skipped.Add(mi);
        continue; // continue so that all files are added to skipped files
      }

      _dlg.Progress.Report(new object[] { 0, srcPath, targetPath, srcFileName });

      if (ResolveTargetFileName(src, target, srcFileName, targetPath, ref mi) is not var (targetFilePath, targetFileName))
        continue;

      try {
        CopyMoveFileOnDrive(srcFilePath, targetFilePath);
      }
      catch (Exception) {
        if (mi != null) Skipped.Add(mi);
        continue;
      }

      CopyMoveCacheOnDrive(mi, srcPathCache, targetPathCache, targetFileName);
    }

    if (src.HasSubFolders) // recursive copy/move
      foreach (var srcSub in src.Items.OfType<FolderM>())
        CopyMoveFolderRecursive(srcSub, target);

    return Task.CompletedTask;
  }

  private void CopyMoveFolderInDBRecursive(FolderM src, FolderM dest) {
    CopyMoveCommonInDB();
    switch (mode) {
      case FileOperationMode.Copy: CopyMoveFolderInDBRecursive(src, dest, MediaItemCopy, null); break;
      case FileOperationMode.Move: CopyMoveFolderInDBRecursive(src, dest, MediaItemMove, DeleteEmptyFolder); break;
    }
  }

  private void CopyMoveMediaItemsInDB(IEnumerable<RealMediaItemM> items, FolderM dest) {
    CopyMoveCommonInDB();
    switch (mode) {
      case FileOperationMode.Copy: CopyMoveMediaItemsInDB(items, dest, MediaItemCopy); break;
      case FileOperationMode.Move: CopyMoveMediaItemsInDB(items, dest, MediaItemMove); break;
    }
  }

  private void CopyMoveCommonInDB() {
    coreR.MediaItem.ItemsDelete(_replaced.ToArray());
    if (mode != FileOperationMode.Move) return;
    foreach (var (mi, newFileName) in _renamed)
      mi.FileName = newFileName;
  }

  private void CopyMoveFolderInDBRecursive(FolderM src, FolderM dest, Action<RealMediaItemM, FolderM> itemAction, Action<FolderM> folderAction) {
    if (dest.GetByName(src.Name) is not { } targetFolder) return;
    CopyMoveMediaItemsInDB(src.MediaItems, targetFolder, itemAction);

    foreach (var subFolder in src.Items.OfType<FolderM>().ToArray())
      CopyMoveFolderInDBRecursive(subFolder, targetFolder, itemAction, folderAction);

    folderAction?.Invoke(src);
  }

  private void DeleteEmptyFolder(FolderM folder) {
    if (!IOExtensions.DeleteDirectoryIfEmpty(folder.FullPath)) return;
    IOExtensions.DeleteDirectoryIfEmpty(folder.FullPathCache);
    coreR.Folder.TreeItemDelete(folder);
  }

  private void CopyMoveMediaItemsInDB(IEnumerable<RealMediaItemM> items, FolderM dest, Action<RealMediaItemM, FolderM> action) =>
    items.Except(Skipped).Cast<RealMediaItemM>().ToList().ForEach(mi => action(mi, dest));

  private Task CopyMoveMediaItemsFiles(IReadOnlyList<RealMediaItemM> items, FolderM dest) {
    var count = items.Count;
    var targetPathCache = dest.FullPathCache;

    for (int i = 0; i < count; i++) {
      var mi = items[i];
      if (_dlg.WorkCts.Token.IsCancellationRequested) {
        Skipped.Add(mi);
        continue; // continue so that all files are added to skipped files
      }

      var srcPath = mi.Folder.FullPath;
      var srcPathCache = mi.Folder.FullPathCache;
      var targetPath = dest.FullPath;

      _dlg.Progress.Report(new object[] { Convert.ToInt32((double)i / count * 100), srcPath, targetPath, mi.FileName });

      if (ResolveTargetFileName(mi.Folder, dest, mi.FileName, targetPath, ref mi) is not var (targetFilePath, targetFileName))
        continue;

      try {
        CopyMoveFileOnDrive(IOExtensions.PathCombine(srcPath, mi.FileName), targetFilePath);
      }
      catch (Exception) {
        Skipped.Add(mi);
        continue;
      }

      CopyMoveCacheOnDrive(mi, srcPathCache, targetPathCache, targetFileName);
    }

    return Task.CompletedTask;
  }

  private (string, string)? ResolveTargetFileName(FolderM src, FolderM target, string srcFileName, string targetPath, ref RealMediaItemM mi) {
    var targetFileName = srcFileName;
    var targetFilePath = IOExtensions.PathCombine(targetPath, targetFileName);
    if (!File.Exists(targetFilePath)) return new(targetFilePath, targetFileName);

    if (mi == null) mi = CreateMediaItemAndReadMetadata(src, srcFileName);
    RealMediaItemM replacedMi = null;
    var result = FileOperationCollisionDialogM.Open(src, target, mi, ref targetFileName, ref replacedMi);

    switch (result) {
      case CollisionResult.Rename: {
        if (mi != null) _renamed.Add(mi, targetFileName);
        targetFilePath = IOExtensions.PathCombine(targetPath, targetFileName);
        break;
      }
      case CollisionResult.Replace: {
        if (replacedMi != null) _replaced.Add(replacedMi);
        break;
      }
      case CollisionResult.Skip: {
        if (mi != null) Skipped.Add(mi);
        return null;
      }
    }

    return new(targetFilePath, targetFileName);
  }

  private void CopyMoveFileOnDrive(string src, string dest) {
    switch (mode) {
      case FileOperationMode.Copy:
        File.Copy(src, dest, true);
        break;
      case FileOperationMode.Move: {
        if (File.Exists(dest)) File.Delete(dest);
        if (dest != null) File.Move(src, dest);
        break;
      }
    }
  }

  private void CopyMoveCacheOnDrive(MediaItemM mi, string srcPathCache, string targetPathCache, string targetFileName) {
    if (mi == null) return;
    var mis = mi is VideoM vid ? [..vid.GetVideoItems()] : new List<MediaItemM>();
    mis.Add(mi);

    var fileNames = new List<(string, string)>(
      mis.Select(x => (x.FileNameCache(x.FileName), x.FileNameCache(targetFileName))));
    
    if (mode == FileOperationMode.Move)
      fileNames.AddRange(mis.GetSegments().Select(x => (x.FileNameCache, x.FileNameCache)));

    try {
      foreach (var fileName in fileNames) {
        var src = IOExtensions.PathCombine(srcPathCache, fileName.Item1);
        if (!File.Exists(src)) continue;
        Directory.CreateDirectory(targetPathCache);
        CopyMoveFileOnDrive(src, IOExtensions.PathCombine(targetPathCache, fileName.Item2));
      }
    }
    catch (Exception) {
      // ignored
    }
  }

  private void CopySegmentsCacheOnDrive() {
    if (mode != FileOperationMode.Copy) return;
    try {
      foreach (var folder in _segments.GroupBy(x => x.Item2.MediaItem.Folder)) {
        Directory.CreateDirectory(folder.Key.FullPathCache);
        foreach (var (srcS, destS) in folder) {
          var src = srcS.FilePathCache;
          if (!File.Exists(src)) continue;
          CopyMoveFileOnDrive(src, destS.FilePathCache);
        }
      }
    }
    catch (Exception) {
      // ignored
    }
  }

  public static RealMediaItemM CreateMediaItemAndReadMetadata(FolderM folder, string fileName) {
    if (Core.R.MediaItem.ItemCreate(folder, fileName) is not { } mi) return null;
    var mim = new MediaItemMetadata(mi);
    MediaItemS.ReadMetadata(mim, false);
    if (mim.Success) mim.FindRefs().Wait();
    return mi;
  }

  private void MediaItemCopy(RealMediaItemM mi, FolderM folder) {
    var fileName = _renamed.TryGetValue(mi, out var newFileName) ? newFileName : mi.FileName;
    var copy = folder.MediaItems.GetByFileName(fileName) ?? coreR.MediaItem.ItemCreate(folder, fileName);
    if (copy == null) return;

    copy.Width = mi.Width;
    copy.Height = mi.Height;
    copy.Orientation = mi.Orientation;
    copy.IsOnlyInDb = mi.IsOnlyInDb;
    MediaItemCopyCommon(mi, copy);
    MediaItemVideoCopyClips(mi as VideoM, copy as VideoM);
    MediaItemVideoCopyImages(mi as VideoM, copy as VideoM);
  }

  private void MediaItemVideoCopyClips(VideoM vid, VideoM copy) {
    if (vid?.VideoClips == null) return;
    copy.VideoClips = [];
    foreach (var vc in vid.VideoClips) {
      var vcCopy = coreR.VideoClip.CustomItemCreate(copy, vc.TimeStart);
      vcCopy.TimeEnd = vc.TimeEnd;
      vcCopy.Volume = vc.Volume;
      vcCopy.Speed = vc.Speed;
      MediaItemCopyCommon(vc, vcCopy);
    }
  }

  private void MediaItemVideoCopyImages(VideoM vid, VideoM copy) {
    if (vid?.VideoImages == null) return;
    copy.VideoImages = [];
    foreach (var vi in vid.VideoImages) {
      var viCopy = coreR.VideoImage.CustomItemCreate(copy, vi.TimeStart);
      MediaItemCopyCommon(vi, viCopy);
    }
  }

  private void MediaItemCopyCommon(MediaItemM mi, MediaItemM copy) {
    copy.Rating = mi.Rating;
    copy.Comment = mi.Comment;

    if (mi.GeoLocation != null) {
      copy.GeoLocation = mi.GeoLocation;
      coreR.MediaItemGeoLocation.IsModified = true;
    }

    if (mi.People != null)
      copy.People = [..mi.People];

    if (mi.Keywords != null)
      copy.Keywords = [..mi.Keywords];

    if (mi.Segments != null)
      foreach (var segment in mi.Segments)
        _segments.Add(new(segment, coreR.Segment.ItemCopy(segment, copy)));
  }

  private void MediaItemMove(RealMediaItemM item, FolderM folder) {
    item.Folder.MediaItems.Remove(item);
    item.Folder = folder;
    item.Folder.MediaItems.Add(item);
    coreR.MediaItem.ModifyOnlyDA(item);
  }
}