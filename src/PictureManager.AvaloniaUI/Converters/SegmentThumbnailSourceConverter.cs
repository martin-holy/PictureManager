using Avalonia;
using Avalonia.Media.Imaging;
using MH.UI.AvaloniaUI.Converters;
using MH.UI.AvaloniaUI.Extensions;
using MH.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace PictureManager.AvaloniaUI.Converters;

public sealed class SegmentThumbnailSourceConverter : BaseMultiConverter, IImageSourceConverter<SegmentM> {
  private static readonly object _lock = new();
  private static SegmentThumbnailSourceConverter? _inst;
  public static SegmentThumbnailSourceConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  private readonly TaskQueue<SegmentM> _taskQueue;

  public HashSet<SegmentM> ErrorCache { get; } = [];
  public HashSet<SegmentM> IgnoreCache { get; } = [];

  public SegmentThumbnailSourceConverter() {
    _taskQueue = new(8, _createThumbnailFromImage, TriggerChanged);
  }

  public override object? Convert(IList<object?> values, object? parameter) {
    try {
      if (values is not [_, SegmentM segment]) return null;
      if (ErrorCache.Contains(segment)) return null;

      if (!File.Exists(segment.FilePathCache)) {
        if (!File.Exists(segment.MediaItem.FilePath)) {
          Tasks.RunOnUiThread(() => Core.R.MediaItem.ItemDelete(segment.MediaItem));
          return null;
        }
        _createThumbnail(segment);

        return null;
      }

      return new Bitmap(segment.FilePathCache);

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

  private void _createThumbnail(SegmentM segment) {
    IgnoreCache.Add(segment);
    if (segment.MediaItem is not ImageM) return;
    _taskQueue.Add(segment);
    _taskQueue.Start();
  }

  private void _createThumbnailFromImage(SegmentM segment) {
    var filePath = segment.MediaItem.FilePath;
    var rect = new Rect(
      (int)segment.X,
      (int)segment.Y,
      (int)segment.Size,
      (int)segment.Size);

    try {
      BitmapExtensions
        .Create(filePath, rect)
        .Resize(SegmentVM.SegmentSize)
        .SaveAsJpeg(segment.FilePathCache, Core.Settings.Common.JpegQuality);
    }
    catch (Exception ex) {
      ErrorCache.Add(segment);
      Log.Error(ex, $"{ex.Message} ({filePath})");
    }
  }

  private static void TriggerChanged(SegmentM segment) =>
    segment.OnPropertyChanged(nameof(segment.FilePathCache));
}