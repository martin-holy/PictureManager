using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Converters;
using PictureManager.Domain;
using PictureManager.Domain.Services;
using PictureManager.Domain.ViewModels.Entities;
using PictureManager.ShellStuff;
using System.Collections.Generic;
using System.Windows;

namespace PictureManager;

public sealed class AppCore : ObservableObject {
  public PictureManager.ViewModels.SegmentRectVM SegmentRectVM { get; private set; }

  public static RelayCommand TestButtonCommand { get; } = new(Tests.Run, Res.IconBug, "Test Button");

  public AppCore() {
    Core.FileOperationDelete = FileOperationDelete;
    Core.GetDisplayScale = GetDisplayScale;

    MH.UI.WPF.Utils.Init.SetDelegates();
    MH.UI.WPF.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    GroupByDialogDataTemplateSelector.TypeToKey = Res.TypeToGroupByDialogTemplateKey;

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    ImageS.WriteMetadata = ViewModels.MediaItemVM.WriteMetadata;
    Core.VideoDetail.GetVideoMetadataFunc = FileInformation.GetVideoMetadata;
    Core.UiFullVideo = new MediaPlayer();
    Core.UiDetailVideo = new MediaPlayer();
    Core.VideoFrameSaver = new VideoFrameSaver();

    SegmentVM.ThumbConverter = SegmentThumbnailSourceConverter.Inst;
    MediaItemVM.ThumbConverter = MediaItemThumbSourceConverter.Inst;
  }

  public void AfterInit() {
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
    (MediaPlayer)(Core.VM.MainWindow.IsInViewMode ? Core.UiFullVideo : Core.UiDetailVideo);
}