﻿using Avalonia.Media.Imaging;
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

  private readonly TaskQueue<MediaItemM> _taskQueue = new(8, CreateImageThumbnail, TriggerChanged);

  public HashSet<MediaItemM> ErrorCache { get; } = [];
  public HashSet<MediaItemM> IgnoreCache { get; } = [];

  public override object? Convert(IList<object?> values, object? parameter) {
    try {
      if (values is not [_, MediaItemM mi]) return null;
      if (ErrorCache.Contains(mi)) return null;

      if (!File.Exists(mi.FilePathCache)) {
        if (!File.Exists(mi.FilePath)) {
          Core.R.MediaItem.ItemDelete(mi is VideoItemM vmi ? vmi.Video : mi);
          return null;
        }

        CreateThumbnail(mi);
        return null;
      }

      return new Bitmap(mi.FilePathCache);

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

  private void CreateThumbnail(MediaItemM mi) {
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
        CreateVideoItemThumbnail(vi);
        break;
    }
  }

  private static void CreateImageThumbnail(MediaItemM mi) =>
    Utils.Imaging.CreateImageThumbnail(
      mi.FilePath,
      mi.FilePathCache,
      Core.Settings.MediaItem.ThumbSize,
      Core.Settings.Common.JpegQuality);

  private static void CreateVideoItemThumbnail(VideoItemM vi) {
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

  private static void TriggerChanged(MediaItemM mi) =>
    mi.OnPropertyChanged(nameof(mi.FilePathCache));
}