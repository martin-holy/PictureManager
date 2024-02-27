﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
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

public sealed class MediaItemS(MediaItemR r) : ObservableObject {
  private static readonly string[] _supportedExts = { ".jpg", ".jpeg", ".mp4" };

  public static Action<MediaItemMetadata, bool> ReadMetadata { get; set; }

  public async Task CopyMove(FileOperationMode mode, List<RealMediaItemM> items, FolderM destFolder,
    IProgress<object[]> progress, CancellationToken token) {
    var count = items.Count;
    var done = 0;
    var replaced = new List<MediaItemM>();

    foreach (var mi in items) {
      if (token.IsCancellationRequested) break;
      progress.Report(new object[] { Convert.ToInt32((double)done / count * 100), mi.Folder.FullPath, destFolder.FullPath, mi.FileName });

      var srcFolder = mi.Folder;
      var srcFileName = mi.FileName;
      var destFileName = mi.FileName;
      var destFilePath = IOExtensions.PathCombine(destFolder.FullPath, mi.FileName);

      // if the file with the same name exists in the destination
      // show dialog with options to Rename, Replace or Skip the file
      if (File.Exists(destFilePath)) {
        var result = FileOperationCollisionDialogM.Open(mi.FilePath, destFilePath, ref destFileName);

        if (result == CollisionResult.Skip) {
          _ = Tasks.RunOnUiThread(() => Core.VM.MediaItem.Views.Current.Selected.Set(mi, false));
          continue;
        }

        if (result == CollisionResult.Replace)
          replaced.Add(mi);
      }

      switch (mode) {
        case FileOperationMode.Copy:
          var miCopy = await Tasks.RunOnUiThread(() => r.ItemCopy(mi, destFolder, destFileName));
          CopyFile(mi, miCopy);
          break;

        case FileOperationMode.Move:
          await Tasks.RunOnUiThread(() => r.ItemMove(mi, destFolder, destFileName));
          MoveFile(mi, srcFolder, destFolder, srcFileName, destFileName);
          break;
      }

      done++;
    }

    await Tasks.RunOnUiThread(() => r.ItemsDelete(replaced));
  }

  public void CopyFile(MediaItemM miSrc, MediaItemM miCopy) {
    Directory.CreateDirectory(miCopy.Folder.FullPathCache);
    File.Copy(miSrc.FilePath, miCopy.FilePath, true);
    File.Copy(miSrc.FilePathCache, miCopy.FilePathCache, true);
    if (miSrc.Segments == null) return;
    for (var i = 0; i < miSrc.Segments.Count; i++)
      File.Copy(miSrc.Segments[i].FilePathCache, miCopy.Segments[i].FilePathCache, true);
  }

  public void MoveFile(MediaItemM mi, FolderM srcF, FolderM destF, string srcFileName, string destFileName) {
    var srcFilePath = IOExtensions.PathCombine(srcF.FullPath, srcFileName);
    var destFilePath = IOExtensions.PathCombine(destF.FullPath, destFileName);
    var srcFilePathCache = IOExtensions.PathCombine(srcF.FullPathCache, srcFileName);
    var destFilePathCache = IOExtensions.PathCombine(destF.FullPathCache, destFileName);

    File.Delete(destFilePath);
    File.Move(srcFilePath, destFilePath);

    Directory.CreateDirectory(destF.FullPathCache);
    File.Delete(destFilePathCache);
    if (File.Exists(srcFilePathCache))
      File.Move(srcFilePathCache, destFilePathCache);

    Core.S.Segment.MoveCacheFiles(mi is VideoM vid ? vid.GetAllSegments() : mi.GetSegments(), srcF, destF);
  }

  public void Delete(MediaItemM[] items) =>
    r.ItemsDelete(items);

  public bool Exists(MediaItemM mi) {
    if (mi == null || File.Exists(mi.FilePath)) return true;
    File.Delete(mi.FilePathCache);
    r.ItemsDelete(new[] { mi });
    return false;
  }

  public static bool IsSupportedFileType(string filePath) =>
    _supportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));

  public void OnMetadataReloaded(RealMediaItemM[] items) {
    r.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
    r.RaiseOrientationChanged(items);
  }

  public Task ReloadMetadata(RealMediaItemM mi) {
    var mim = new MediaItemMetadata(mi);
    if (mi is not VideoM) ReadMetadata(mim, false);

    return Tasks.RunOnUiThread(async () => {
      if (mi is VideoM) ReadMetadata(mim, false);
      if (mim.Success) await mim.FindRefs();
      r.Modify(mi);
      mi.IsOnlyInDb = false;
    });
  }

  public void Rename(RealMediaItemM mi, string newFileName) =>
    r.ItemRename(mi, newFileName);

  public void SetComment(MediaItemM mi, string comment) {
    mi.Comment = comment;
    mi.SetInfoBox(true);
    mi.OnPropertyChanged(nameof(mi.Comment));
    r.Modify(mi);
  }
}