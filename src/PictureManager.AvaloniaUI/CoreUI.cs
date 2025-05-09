﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MH.Utils.Extensions;
using PictureManager.AvaloniaUI.Controls;
using PictureManager.AvaloniaUI.Converters;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PictureManager.AvaloniaUI.Utils;
using AUI = MH.UI.AvaloniaUI;

namespace PictureManager.AvaloniaUI;

public class CoreUI : ICoreP {
  public IImagingP ImagingP { get; }

  public CoreUI() {
    CoreR.FileOperationDelete = FileOperationDelete;

    AUI.Utils.Init.SetDelegates();
    AUI.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    AUI.Controls.CollectionViewHost.GroupByDialogDataTemplateSelector = new GroupByDialogDataTemplateSelector();

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    //TODO PORT ImageS.WriteMetadata = ViewModels.MediaItemVM.WriteMetadata;
    //TODO PORT Not supported VideoVM.GetVideoMetadataFunc = FileInformation.GetVideoMetadata;
    CoreVM.DisplayScale = _getDisplayScale();
    //TODO PORT Not supported CoreVM.UiFullVideo = new MediaPlayer();
    //TODO PORT Not supported CoreVM.UiDetailVideo = new MediaPlayer();
    //TODO PORT Not supported CoreVM.VideoFrameSaver = new VideoFrameSaver();
    Core.R.MediaItem.VideoSupport = false;

    //TODO PORT SegmentS.ExportSegment = Utils.Imaging.ExportSegment;
    //TODO PORT SegmentVM.ThumbConverter = SegmentThumbnailSourceConverter.Inst;
    MediaItemVM.ThumbConverter = MediaItemThumbSourceConverter.Inst;

    ImagingP = ImagingPFactory.Create();
  }

  public void AfterInit() {
    // TODO PORT LoadPlugins();
  }

  private static double _getDisplayScale() =>
    Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }
      ? window.DesktopScaling
      : 1.0;

  public static Dictionary<string, string>? FileOperationDelete(List<string> items, bool recycle, bool silent) {
    // TODO PORT recycle
    items.Where(File.Exists).ToList().ForEach(File.Delete);
    return null;
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    Utils.Imaging.CreateImageThumbnail(srcPath, destPath, desiredSize, quality);

  public string GetFilePathCache(FolderM folder, string fileNameCache) =>
    IOExtensions.PathCombine(folder.FullPathCache, fileNameCache);

  public string GetFolderPathCache(FolderM folder) =>
    folder.FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.Common.CachePath);
}