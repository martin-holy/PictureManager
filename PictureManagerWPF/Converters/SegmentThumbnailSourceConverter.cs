using MH.UI.WPF.Converters;
using MH.UI.WPF.Extensions;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters;

public sealed class SegmentThumbnailSourceConverter : BaseMultiConverter, IImageSourceConverter<SegmentM> {
  private static readonly object _lock = new();
  private static SegmentThumbnailSourceConverter _inst;
  public static SegmentThumbnailSourceConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  private static readonly TaskQueue<SegmentM> _taskQueue = new();

  public HashSet<SegmentM> ErrorCache { get; } = new();
  public HashSet<SegmentM> IgnoreCache { get; } = new();

  public override object Convert(object[] values, object parameter) {
    try {
      if (values is not [_, SegmentM segment] || segment.MediaItem == null) return null;
      if (ErrorCache.Contains(segment)) return null;

      if (!File.Exists(segment.FilePathCache)) {
        if (!File.Exists(segment.MediaItem.FilePath)) {
          Tasks.RunOnUiThread(() => Core.Db.MediaItems.ItemDelete(segment.MediaItem));
          return null;
        }
        CreateThumbnail(segment);

        return null;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(segment.FilePathCache);
      src.Rotation = Utils.Imaging.MediaOrientation2Rotation((MediaOrientation)segment.MediaItem.Orientation);

      if (IgnoreCache.Remove(segment))
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

  private void CreateThumbnail(SegmentM segment) {
    IgnoreCache.Add(segment);

    if (segment.MediaItem is ImageM) {
      _taskQueue.Add(segment);
      _taskQueue.Start(CreateThumbnailFromImage, TriggerChanged);
    }
    else
      CreateThumbnailFromVideo(segment);
  }

  private void CreateThumbnailFromImage(SegmentM segment) {
    var filePath = segment.MediaItem.FilePath;
    var rect = new Int32Rect(
      (int)segment.X,
      (int)segment.Y,
      (int)segment.Size,
      (int)segment.Size);

    try {
      BitmapSourceExtensions
        .Create(filePath, rect)
        .Resize(SegmentsM.SegmentSize)
        .SaveAsJpeg(segment.FilePathCache, Core.Settings.JpegQualityLevel);
    }
    catch (Exception ex) {
      ErrorCache.Add(segment);
      Log.Error(ex, $"{ex.Message} ({filePath})");
    }
  }

  private static void CreateThumbnailFromVideo(SegmentM segment) =>
    Core.VideoThumbsM.Create(new[] { segment.MediaItem });

  private static void TriggerChanged(SegmentM segment) =>
    segment.OnPropertyChanged(nameof(segment.FilePathCache));
}