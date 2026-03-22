using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Common;

public sealed class FileOperationCollisionDialog : Dialog {
  private readonly FolderM _destFolder;

  private string _error = string.Empty;
  private string _fileName;
  private FileCollisionInfo _dstFile;

  public string Error { get => _error; set { _error = value; OnPropertyChanged(); } }
  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public FileCollisionInfo SrcFile { get; }
  public FileCollisionInfo DstFile { get => _dstFile; set { _dstFile = value; OnPropertyChanged(); } }

  public FileOperationCollisionDialog(FolderM srcFolder, FolderM dstFolder, RealMediaItemM? srcMi, string fileName) : base("File already exists", Res.IconImageMultiple) {
    _destFolder = dstFolder;
    SrcFile = new(srcMi, IOExtensions.PathCombine(srcFolder.FullPath, fileName));
    _dstFile = new(_getMediaItem(dstFolder, fileName), IOExtensions.PathCombine(dstFolder.FullPath, fileName));
    _fileName = fileName;

    Buttons = [
      new(new RelayCommand(_rename, null, "Rename")),
      new(new RelayCommand(() => Result = (int)CollisionResult.Replace, null, "Replace")),
      new(new RelayCommand(() => Result = (int)CollisionResult.Skip, null, "Skip"))
    ]; 
  }

  public static async Task<(CollisionResult, string, RealMediaItemM?)> Open(FolderM src, FolderM dest, RealMediaItemM? srcMi, string fileName, RealMediaItemM? replacedMi) {
    var result = CollisionResult.Skip;
    var outFileName = fileName;
    var outReplacedMi = replacedMi;

    _initThumb(srcMi);

    await Tasks.RunOnUiThread(async () => {
      var cd = new FileOperationCollisionDialog(src, dest, srcMi, outFileName);
      result = (CollisionResult)await ShowAsync(cd);
      outFileName = cd.FileName;
      outReplacedMi = cd.DstFile.MediaItem;
    });

    return new(result, outFileName, outReplacedMi);
  }

  private static void _initThumb(RealMediaItemM? mi) {
    if (mi == null) return;
    mi.SetInfoBox();
    mi.SetThumbSize();
  }

  private static RealMediaItemM? _getMediaItem(FolderM folder, string fileName) {
    var mi = folder.MediaItems.GetByFileName(fileName)
             ?? CopyMoveU.CreateMediaItemAndReadMetadata(folder, fileName);

    _initThumb(mi);
    return mi;
  }

  private void _rename() {
    Error = string.Empty;

    if (string.IsNullOrEmpty(FileName)) {
      Error = "New file name is empty!";
      return;
    }

    if (Path.GetInvalidFileNameChars().Any(FileName.Contains)) {
      Error = "New file name contains incorrect character(s)!";
      return;
    }

    var newFilePath = Path.Combine(_destFolder.FullPath, FileName);
    if (File.Exists(newFilePath)) {
      DstFile = new(_getMediaItem(_destFolder, FileName), newFilePath);
      return;
    }

    Result = (int)CollisionResult.Rename;
  }
}

public sealed class FileCollisionInfo {
  public RealMediaItemM? MediaItem { get; }
  public string Path { get; } = string.Empty;
  public string Size { get; } = string.Empty;
  public string LastWrite { get; } = string.Empty;

  public FileCollisionInfo(RealMediaItemM? mediaItem, string filePath) {
    MediaItem = mediaItem;

    try {
      var fi = new FileInfo(filePath);
      Path = fi.FullName;
      Size = fi.Length.ToString("0 B");
      LastWrite = fi.LastWriteTime.ToString("G");
    }
    catch (Exception ex) {
      MH.Utils.Log.Error(ex);
    }
  }
}