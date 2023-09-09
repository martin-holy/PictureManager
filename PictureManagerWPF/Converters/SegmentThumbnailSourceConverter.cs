using MH.UI.WPF.Converters;
using MH.UI.WPF.Utils;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters; 

public class SegmentThumbnailSourceConverter : BaseMarkupExtensionMultiConverter {
  public static SegmentM IgnoreImageCacheSegment { get; set; }

  private static bool _isRunning;
  private static readonly List<SegmentM> _segmentsQueue = new();

  public override object Convert(object[] values, object parameter) {
    try {
      if (values is not [_, SegmentM segment]) return null;

      if (!File.Exists(segment.FilePathCache)) {
        if (!_segmentsQueue.Contains(segment))
          _segmentsQueue.Add(segment);

        if (!_isRunning)
          StartQueue();

        return null;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(segment.FilePathCache);
      src.Rotation = Utils.Imaging.MediaOrientation2Rotation((MediaOrientation)segment.MediaItem.Orientation);

      if (segment.Equals(IgnoreImageCacheSegment)) {
        IgnoreImageCacheSegment = null;
        src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
      }

      src.EndInit();

      return src;
    }
    catch (Exception ex) {
      Debug.WriteLine(ex);
      return null;
    }
  }

  private static async void StartQueue() {
    if (_segmentsQueue.Count == 0) {
      _isRunning = false;
      return;
    }

    _isRunning = true;
    var segments = _segmentsQueue.ToArray();

    await Task.WhenAll(
      from partition in Partitioner.Create(segments).GetPartitions(Environment.ProcessorCount)
      select Task.Run(() => {
        using (partition)
          while (partition.MoveNext()) {
            var segment = partition.Current;
            CreateThumbnail(segment);
            segment?.OnPropertyChanged(nameof(segment.FilePathCache));
          }
        return Task.CompletedTask;
      }));

    foreach (var segment in segments)
      _segmentsQueue.Remove(segment);

    _isRunning = false;
    StartQueue();
  }

  public static void CreateThumbnail(SegmentM segment) {
    var filePath = segment.MediaItem.MediaType == MediaType.Image
      ? segment.MediaItem.FilePath
      : segment.MediaItem.FilePathCache;
    var rect = new Int32Rect(
      (int)segment.X,
      (int)segment.Y,
      (int)segment.Size,
      (int)segment.Size);

    try {
      Imaging.GetCroppedBitmapSource(filePath, rect, SegmentsM.SegmentSize)
        ?.SaveAsJpg(80, segment.FilePathCache);

      IgnoreImageCacheSegment = segment;
      segment.OnPropertyChanged(nameof(segment.FilePathCache));
    }
    catch (Exception ex) {
      Log.Error(ex, filePath);
    }
  }
}