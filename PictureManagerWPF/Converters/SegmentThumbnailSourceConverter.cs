using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.Converters {
  public class SegmentThumbnailSourceConverter : IMultiValueConverter {
    public static SegmentM IgnoreImageCacheSegment { get; set; }

    private static bool _isRunning;
    private static readonly List<SegmentM> _segmentsQueue = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (values?.Length != 2 || values[1] is not SegmentM segment) return null;

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
        src.Rotation = Imaging.MediaOrientation2Rotation((MediaOrientation)segment.MediaItem.Orientation);

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

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

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
              App.Ui.SegmentsVM.CreateThumbnail(segment);
              segment?.OnPropertyChanged(nameof(segment.FilePathCache));
            }
          return Task.CompletedTask;
        }));

      foreach (var segment in segments)
        _segmentsQueue.Remove(segment);

      _isRunning = false;
      StartQueue();
    }
  }
}
