﻿using MH.UI.WPF.Converters;
using MH.UI.WPF.Extensions;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters;

public sealed class MediaItemThumbSourceConverter : BaseMultiConverter {
  private static MediaItemThumbSourceConverter _inst;
  private static readonly object _lock = new();
  public static MediaItemThumbSourceConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  private static readonly TaskQueue<MediaItemM> _taskQueue = new();
  private static readonly HashSet<MediaItemM> _ignoreCache = new();

  public override object Convert(object[] values, object parameter) {
    try {
      if (values is not [_, MediaItemM mi]) return null;

      if (!File.Exists(mi.FilePathCache)) {
        if (!File.Exists(mi.FilePath)) {
          Core.Db.MediaItems.ItemDelete(mi is VideoItemM vmi ? vmi.Video : mi);
          return null;
        }

        CreateThumbnail(mi);
        return null;
      }

      var orientation = mi.Orientation;
      // swap 90 and 270 degrees for video
      // TODO test it for VideoClip and VideoImage
      if (mi is VideoM) {
        if (mi.Orientation == 6)
          orientation = 8;
        else if (mi.Orientation == 8)
          orientation = 6;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(mi.FilePathCache);
      src.Rotation = Utils.Imaging.MediaOrientation2Rotation((MediaOrientation)orientation);

      if (_ignoreCache.Remove(mi))
        src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

      src.EndInit();

      return src;
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

  private static void CreateThumbnail(MediaItemM mi) {
    _ignoreCache.Add(mi);

    switch (mi) {
      case ImageM:
        _taskQueue.Add(mi);
        _taskQueue.Start(CreateImageThumbnail, TriggerChanged);
        break;
      case VideoM:
        Core.VideoThumbsM.Create(new[] { mi });
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
      Core.Settings.ThumbnailSize,
      Core.Settings.JpegQualityLevel);

  private static void CreateVideoItemThumbnail(VideoItemM vi) {
    if (ReferenceEquals(Core.VideoDetail.MediaPlayer.CurrentItem, vi)) {
      AppCore.CurrentMediaPlayer()
        .ToBitmap()
        .Resize(Core.Settings.ThumbnailSize)
        .SaveAsJpeg(vi.FilePathCache, Core.Settings.JpegQualityLevel);

      TriggerChanged(vi);
    }
    else
      Core.VideoThumbsM.Create(new[] { (MediaItemM)vi.Video });
  }

  private static void TriggerChanged(MediaItemM mi) =>
    mi.OnPropertyChanged(nameof(mi.FilePathCache));
}