using MH.UI.WPF.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Layout;
using PictureManager.Windows.WPF.Converters;
using PictureManager.Windows.WPF.ShellStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;

namespace PictureManager.Windows.WPF;

public sealed class CoreUI : ObservableObject, ICoreP {
  public WPF.ViewModels.SegmentRectUiVM SegmentRectUiVM { get; private set; } = null!;

  public static RelayCommand TestButtonCommand { get; } = new(Tests.Run, Res.IconBug, "Test Button");

  public CoreUI() {
    CoreR.FileOperationDelete = FileOperationDelete;

    MH.UI.WPF.Utils.Init.SetDelegates();
    MH.UI.WPF.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    GroupByDialogDataTemplateSelector.TypeToKey = Res.TypeToGroupByDialogTemplateKey;

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    ImageS.WriteMetadata = ViewModels.MediaItemVM.WriteMetadata;
    VideoVM.GetVideoMetadataFunc = FileInformation.GetVideoMetadata;
    CoreVM.DisplayScale = GetDisplayScale();
    CoreVM.UiFullVideo = new MediaPlayer();
    CoreVM.UiDetailVideo = new MediaPlayer();
    CoreVM.VideoFrameSaver = new VideoFrameSaver();

    SegmentS.ExportSegment = Utils.Imaging.ExportSegment;
    SegmentVM.ThumbConverter = SegmentThumbnailSourceConverter.Inst;
    MediaItemVM.ThumbConverter = MediaItemThumbSourceConverter.Inst;
  }

  public void AfterInit() {
    LoadPlugins();
    SegmentRectUiVM = new(Core.VM.Segment.Rect, new(Core.S.Segment));

    Core.R.Segment.ItemDeletedEvent += _onSegmentItemDeleted;

    this.Bind(Core.VM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem,
      (t, p) => { if (p) t.SegmentRectUiVM.SegmentRectS.ReloadMediaItemSegmentRects(); });

    this.Bind(Core.VM.MainWindow, nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, (t, p) => {
      if (p) t.SegmentRectUiVM.SegmentRectS.SetMediaItem(Core.VM.MediaItem.Current, Core.VM.Segment.Rect.ShowOverMediaItem);
    });

    this.Bind(Core.VM.MediaItem, nameof(MediaItemVM.Current), x => x.Current, (t, p) => {
      if (Core.VM.MainWindow.IsInViewMode)
        t.SegmentRectUiVM.SegmentRectS.SetMediaItem(p, Core.VM.Segment.Rect.ShowOverMediaItem);
    });

    this.Bind(Core.VM.MediaViewer.ZoomAndPan, nameof(MH.UI.Controls.ZoomAndPan.ScaleX), x => x.ScaleX,
      (t, p) => t.SegmentRectUiVM.SegmentRectS.UpdateScale(p));

    this.Bind(Core.VM.Video.MediaPlayer, nameof(MH.UI.Controls.MediaPlayer.TimelinePosition), x => x.TimelinePosition, (t, p) => {
      var vm = Core.VM;
      if (!vm.Segment.Rect.ShowOverMediaItem || !vm.MediaViewer.IsVisible || vm.MediaItem.Current == null) return;

      var pos = vm.MediaItem.Current is VideoItemM vi ? vi.TimeStart : 0;
      MediaItemM? mi = vm.Video.MediaPlayer.TimelinePosition == pos ? vm.MediaItem.Current : null;
      t.SegmentRectUiVM.SegmentRectS.SetMediaItem(mi, true);
    });
  }

  private static double GetDisplayScale() =>
    Application.Current.MainWindow == null
      ? 1.0
      : PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

  public static Dictionary<string, string>? FileOperationDelete(List<string> items, bool recycle, bool silent) {
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
        var assembly = Assembly.LoadFrom(pluginPath);
        var resourceManager = new ResourceManager($"{asmName}.g", assembly);

        if (resourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, true) is not { } resourceSet) continue;

        var uris = resourceSet.Cast<DictionaryEntry>()
          .Select(x => x.Key.ToString())
          .Where(x => x != null && (x.StartsWith("resources/") || x.StartsWith("views/")) && x.EndsWith(".baml"))
          .Select(x => new Uri($"/{asmName};component/{x!.Replace(".baml", ".xaml")}", UriKind.Relative))
          .ToArray();
        
        foreach (var uri in uris) {
          if (Application.LoadComponent(uri) is ResourceDictionary dict)
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    Utils.Imaging.CreateImageThumbnail(srcPath, destPath, desiredSize, quality);

  public string GetFilePathCache(FolderM folder, string fileNameCache) =>
    IOExtensions.PathCombine(folder.FullPathCache, fileNameCache);

  public string GetFolderPathCache(FolderM folder) =>
    folder.FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.Common.CachePath);

  private void _onSegmentItemDeleted(object? sender, SegmentM e) {
    SegmentRectUiVM.SegmentRectS.RemoveIfContains(e);
  }
}