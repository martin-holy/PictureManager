using MH.UI.AvaloniaUI.Converters;
using MH.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace PictureManager.AvaloniaUI.Converters;

public sealed class MediaItemThumbSourceConverter : BaseMultiConverter, IImageSourceConverter<MediaItemM> {
  private static readonly object _lock = new();
  private static MediaItemThumbSourceConverter? _inst;
  public static MediaItemThumbSourceConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  private readonly TaskQueue<MediaItemM> _taskQueue =
    new(Environment.ProcessorCount, _createImageThumbnail, _triggerChanged);

  public HashSet<MediaItemM> ErrorCache { get; } = [];
  public HashSet<MediaItemM> IgnoreCache { get; } = [];

  public override object? Convert(IList<object?> values, object? parameter) {
    if (values is not [_, MediaItemM mi]) return null;
    if (ErrorCache.Contains(mi)) return null;

    try {
      var thumb = App.CoreUI.ImagingP.GetImageThumbnail(mi);
      if (thumb == null) {
        if (!File.Exists(mi.FilePath)) {
          Core.R.MediaItem.ItemDelete(mi is VideoItemM vmi ? vmi.Video : mi);
          return null;
        }

        _createThumbnail(mi);
        return null;
      }

      return thumb;

      // TODO PORT rotation and cache
    }
    catch (Exception ex) {
      // The process cannot access the file 'xy' because it is being used by another process.
      // thumb file wasn't closed yet after creation
      if (ex.HResult == -2147024864)
        return null;

      Log.Error(ex);
      return null;
    }
  }

  private void _createThumbnail(MediaItemM mi) {
    IgnoreCache.Add(mi);

    switch (mi) {
      case ImageM:
        _taskQueue.Add(mi);
        _taskQueue.Start();
        break;
      case VideoM:
        VideoThumbsU.Create([mi]);
        break;
      case VideoItemM vi:
        _createVideoItemThumbnail(vi);
        break;
    }
  }

  private static void _createImageThumbnail(MediaItemM mi) =>
    App.CoreUI.ImagingP.CreateImageThumbnail(mi);

  private static void _createVideoItemThumbnail(VideoItemM vi) {
    // TODO PORT
    /*if (ReferenceEquals(Core.VM.Video.MediaPlayer.CurrentItem, vi)) {
      AppCore.CurrentMediaPlayer()
        .ToBitmap()
        .Resize(Core.Settings.MediaItem.ThumbSize)
        .SaveAsJpeg(vi.FilePathCache, Core.Settings.Common.JpegQuality);

      TriggerChanged(vi);
    }
    else
      VideoThumbsU.Create([vi.Video]);*/
  }

  private static void _triggerChanged(MediaItemM mi) =>
    mi.OnPropertyChanged(nameof(mi.FilePathCache));
}