using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Converters;
using PictureManager.Domain;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.ViewModels;
using PictureManager.ShellStuff;
using PictureManager.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace PictureManager;

public sealed class AppCore : ObservableObject {
  public SegmentsRectsVM SegmentsRectsVM { get; private set; }

  public static RelayCommand TestButtonCommand { get; } = new(Tests.Run, Res.IconBug, "Test Button");

  public AppCore() {
    Core.FileOperationDelete = FileOperationDelete;
    Core.GetDisplayScale = GetDisplayScale;

    MH.UI.WPF.Utils.Init.SetDelegates();
    MH.UI.WPF.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    GroupByDialogDataTemplateSelector.TypeToKey = Res.TypeToGroupByDialogTemplateKey;

    MediaItemsM.ReadMetadata = ViewModels.MediaItemsVM.ReadMetadata;
    ImagesM.WriteMetadata = ViewModels.MediaItemsVM.WriteMetadata;
    Core.VideoDetail.GetVideoMetadataFunc = FileInformation.GetVideoMetadata;
    Core.UiFullVideo = new MediaPlayer();
    Core.UiDetailVideo = new MediaPlayer();
    Core.VideoFrameSaver = new VideoFrameSaver();

    SegmentsVM.ThumbConverter = SegmentThumbnailSourceConverter.Inst;
    Domain.ViewModels.MediaItemsVM.ThumbConverter = MediaItemThumbSourceConverter.Inst;
  }

  public void AfterInit() {
    SegmentsRectsVM = new(Core.M.Segments.SegmentsRectsM);
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