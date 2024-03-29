﻿using MH.UI.WPF.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using PictureManager.Common.ViewModels;
using PictureManager.Common.ViewModels.Entities;
using PictureManager.Windows.WPF.Converters;
using PictureManager.Windows.WPF.ShellStuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PictureManager.Windows.WPF;

public sealed class AppCore : ObservableObject {
  public WPF.ViewModels.SegmentRectVM SegmentRectVM { get; private set; }

  public static RelayCommand TestButtonCommand { get; } = new(Tests.Run, Res.IconBug, "Test Button");

  public AppCore() {
    CoreR.FileOperationDelete = FileOperationDelete;
    Core.GetDisplayScale = GetDisplayScale;

    MH.UI.WPF.Utils.Init.SetDelegates();
    MH.UI.WPF.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    GroupByDialogDataTemplateSelector.TypeToKey = Res.TypeToGroupByDialogTemplateKey;

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    ImageS.WriteMetadata = ViewModels.MediaItemVM.WriteMetadata;
    VideoVM.GetVideoMetadataFunc = FileInformation.GetVideoMetadata;
    CoreVM.UiFullVideo = new MediaPlayer();
    CoreVM.UiDetailVideo = new MediaPlayer();
    CoreVM.VideoFrameSaver = new VideoFrameSaver();

    SegmentVM.ThumbConverter = SegmentThumbnailSourceConverter.Inst;
    MediaItemVM.ThumbConverter = MediaItemThumbSourceConverter.Inst;
  }

  public void AfterInit() {
    LoadPlugins();
    SegmentRectVM = new(Core.S.Segment.Rect);
  }

  private static double GetDisplayScale() =>
    Application.Current.MainWindow == null
      ? 1.0
      : PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

  public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
    if (items.Count == 0) return null;
    var fops = new PicFileOperationProgressSink();
    using var fo = new FileOperation(fops);
    fo.SetOperationFlags(
      (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
      (silent
        ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
          FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
        : FileOperationFlags.FOF_NOCONFIRMMKDIR));

    foreach (var x in items)
      fo.DeleteItem(x);
    fo.PerformOperations();

    return fops.FileOperationResult;
  }

  public static MediaPlayer CurrentMediaPlayer() =>
    (MediaPlayer)(Core.VM.MainWindow.IsInViewMode ? CoreVM.UiFullVideo : CoreVM.UiDetailVideo);

  private static void LoadPlugins() {
    foreach (var plugin in Core.Inst.Plugins) {
      try {
        var asmName = $"{plugin.Name}.Windows.WPF";
        var pluginPath = Path.GetFullPath(Path.Combine("plugins", plugin.Name, $"{asmName}.dll"));
        var uri = new Uri($"/{asmName};component/resources/main.xaml", UriKind.Relative);
        Assembly.LoadFrom(pluginPath);
        if (Application.LoadComponent(uri) as ResourceDictionary is not { } dict) continue;
        Application.Current.Resources.MergedDictionaries.Add(dict);
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }
  }
}