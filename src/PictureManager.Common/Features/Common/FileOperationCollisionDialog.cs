﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Utils;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Features.Common;

public sealed class FileOperationCollisionDialog : Dialog {
  private readonly FolderM _destFolder;

  private string _error = string.Empty;
  private string _fileName;
  private RealMediaItemM? _srcMediaItem;
  private RealMediaItemM? _destMediaItem;
  private string _srcPath = null!;
  private string _destPath = null!;
  private string _srcSize = null!;
  private string _destSize = null!;
  private string _srcLastWrite = null!;
  private string _destLastWrite = null!;

  public string Error { get => _error; set { _error = value; OnPropertyChanged(); } }
  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public RealMediaItemM? SrcMediaItem { get => _srcMediaItem; set { _srcMediaItem = value; OnPropertyChanged(); } }
  public RealMediaItemM? DestMediaItem { get => _destMediaItem; set { _destMediaItem = value; OnPropertyChanged(); } }
  public string SrcPath { get => _srcPath; set { _srcPath = value; OnPropertyChanged(); } }
  public string DestPath { get => _destPath; set { _destPath = value; OnPropertyChanged(); } }
  public string SrcSize { get => _srcSize; set { _srcSize = value; OnPropertyChanged(); } }
  public string DestSize { get => _destSize; set { _destSize = value; OnPropertyChanged(); } }
  public string SrcLastWrite { get => _srcLastWrite; set { _srcLastWrite = value; OnPropertyChanged(); } }
  public string DestLastWrite { get => _destLastWrite; set { _destLastWrite = value; OnPropertyChanged(); } }

  public FileOperationCollisionDialog(FolderM src, FolderM dest, RealMediaItemM? srcMi, string fileName) : base("The destination already has a file with this name", Res.IconImageMultiple) {
    _destFolder = dest;
    SetInfo(IOExtensions.PathCombine(src.FullPath, fileName), true);
    SetInfo(IOExtensions.PathCombine(dest.FullPath, fileName), false);
    _srcMediaItem = srcMi;
    _destMediaItem = GetMediaItem(dest, fileName);
    _fileName = fileName;

    Buttons = [
      new(new RelayCommand(Rename, null, "Rename")),
      new(new RelayCommand(() => Result = (int)CollisionResult.Replace, null, "Replace")),
      new(new RelayCommand(() => Result = (int)CollisionResult.Skip, null, "Skip"))
    ]; 
  }

  public static CollisionResult Open(FolderM src, FolderM dest, RealMediaItemM? srcMi, ref string fileName, ref RealMediaItemM? replacedMi) {
    var result = CollisionResult.Skip;
    var outFileName = fileName;
    var outReplacedMi = replacedMi;

    InitThumb(srcMi);

    Tasks.RunOnUiThread(() => {
      var cd = new FileOperationCollisionDialog(src, dest, srcMi, outFileName);
      result = (CollisionResult)Show(cd);
      outFileName = cd.FileName;
      outReplacedMi = cd.DestMediaItem;
    }).GetAwaiter().GetResult();

    fileName = outFileName;
    replacedMi = outReplacedMi;

    return result;
  }

  private static void InitThumb(RealMediaItemM? mi) {
    if (mi == null) return;
    mi.SetInfoBox();
    mi.SetThumbSize();
  }

  private static RealMediaItemM? GetMediaItem(FolderM folder, string fileName) {
    var mi = folder.MediaItems.GetByFileName(fileName)
             ?? CopyMoveU.CreateMediaItemAndReadMetadata(folder, fileName);

    InitThumb(mi);
    return mi;
  }

  private void Rename() {
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
      SetInfo(newFilePath, false);
      DestMediaItem = GetMediaItem(_destFolder, FileName);
      return;
    }

    Result = (int)CollisionResult.Rename;
  }

  private void SetInfo(string path, bool src) {
    var fi = new FileInfo(path);
    var size = fi.Length.ToString("0 B");
    var lastWrite = fi.LastWriteTime.ToString("G");
    if (src) {
      SrcPath = fi.FullName;
      SrcSize = size;
      SrcLastWrite = lastWrite;
    }
    else {
      DestPath = fi.FullName;
      DestSize = size;
      DestLastWrite = lastWrite;
    }
  }
}