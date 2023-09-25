using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.ShellStuff;
using PictureManager.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace PictureManager {
  public sealed class AppCore : ObservableObject {
    public MediaItemsVM MediaItemsVM { get; }
    public SegmentsRectsVM SegmentsRectsVM { get; }
    public static MediaPlayer FullVideo { get; private set; }

    public static RelayCommand<object> TestButtonCommand { get; } = new(() => Tests.Run());
    public static RelayCommand<RoutedEventArgs> MediaPlayerLoadedCommand { get; } =
      new(e => FullVideo = e.Source as MediaPlayer);

    public AppCore() {
      SetDelegates();

      MH.UI.WPF.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;

      MediaItemsVM = new(App.Core, Core.MediaItemsM);
      SegmentsRectsVM = new(Core.SegmentsM.SegmentsRectsM);
    }

    private static void SetDelegates() {
      Dialog.Show = DialogHost.Show;
      Core.DialogHostShow = DialogHost.Show;
      Core.FileOperationDelete = FileOperationDelete;
      Core.GetDisplayScale = GetDisplayScale;

      Domain.Utils.Imaging.GetHashPixels = Utils.Imaging.GetHashPixels;
      Domain.Utils.Imaging.ResizeJpg = MH.UI.WPF.Utils.Imaging.ResizeJpg;

      MH.UI.WPF.Utils.Init.SetDelegates();
      GroupByDialogDataTemplateSelector.TypeToKey = Res.TypeToGroupByDialogTemplateKey;

      Core.VideoClipsM.CreateThumbnail = Utils.Imaging.CreateVideoClipThumbnail;
      Core.MediaViewerM.GetVideoMetadata = FileInformation.GetVideoMetadata;
    }

    private static double GetDisplayScale() =>
      Application.Current.MainWindow == null
        ? 1.0
        : PresentationSource.FromVisual(Application.Current.MainWindow)
          ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
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
  }
}
