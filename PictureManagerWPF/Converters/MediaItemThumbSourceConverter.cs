using MH.UI.WPF.Converters;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters;

public sealed class MediaItemThumbSourceConverter : BaseMarkupExtensionMultiConverter {
  private static readonly TaskQueue<MediaItemM> _taskQueue = new();
  private static readonly HashSet<MediaItemM> _ignoreCache = new();

  public override object Convert(object[] values, object parameter) {
    try {
      if (values is not [_, MediaItemM mi]) return null;

      if (!File.Exists(mi.FilePathCache)) {
        if (!File.Exists(mi.FilePath)) {
          Tasks.RunOnUiThread(() =>
            Core.Db.MediaItems.ItemsDelete(new[] { mi }));
          return null;
        }

        CreateThumbnail(mi);
        return null;
      }

      var orientation = mi.Orientation;
      // swap 90 and 270 degrees for video
      if (mi.MediaType == MediaType.Video) {
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
    if (mi.MediaType == MediaType.Video) {
      CreateVideoThumbnail(mi);
      TriggerChanged(mi);
    }
    else {
      _taskQueue.Add(mi);
      _taskQueue.Start(CreateImageThumbnail, TriggerChanged);
    }
  }

  private static void CreateVideoThumbnail(MediaItemM mi) =>
    Utils.Imaging.CreateThumbnail(
      mi.FilePath,
      mi.FilePathCache,
      Core.Settings.ThumbnailSize,
      0,
      Core.Settings.JpegQualityLevel);

  private static void CreateImageThumbnail(MediaItemM mi) =>
    Utils.Imaging.CreateImageThumbnail(
      mi.FilePath,
      mi.FilePathCache,
      Core.Settings.ThumbnailSize,
      Core.Settings.JpegQualityLevel);

  private static void TriggerChanged(MediaItemM mi) =>
    mi.OnPropertyChanged(nameof(mi.FilePathCache));
}