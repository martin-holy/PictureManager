using System;
using System.Collections.Generic;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipM : TreeItem {
    private string _title;
    private int _timeStart;
    private int _timeEnd;
    private double _volume;
    private double _speed;

    // DB Fields
    public int Id { get; }
    public MediaItemM MediaItem { get; set; }
    public int TimeStart { get => _timeStart; set { _timeStart = value; OnPropertyChanged(); } }
    public int TimeEnd { get => _timeEnd; set { _timeEnd = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); } }
    public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
    public int? Rating { get; set; }
    public string Comment { get; set; }
    public List<PersonM> People { get; set; }
    public List<KeywordM> Keywords { get; set; }

    public string TimeStartStr => FormatPosition(TimeStart);
    public string TimeEndStr => FormatPosition(TimeEnd);
    public string DurationStr => FormatDuration(TimeEnd - TimeStart);
    public string VolumeStr => $"{(int)(Volume * 100)}%";
    public string SpeedStr => $"{Speed}x";
    public string ThumbPath => GetThumbPath();

    public VideoClipM(int id, MediaItemM mediaItem) {
      Id = id;
      MediaItem = mediaItem;
    }

    private static string FormatPosition(int ms) =>
      TimeSpan.FromMilliseconds(ms).ToString(
        ms >= 60 * 60 * 1000
          ? @"h\:mm\:ss\.fff"
          : @"m\:ss\.fff");

    private static string FormatDuration(int ms) =>
      ms < 0
        ? string.Empty
        : TimeSpan.FromMilliseconds(ms).ToString(
          ms >= 60 * 60 * 1000
            ? @"h\:mm\:ss\.f"
            : ms >= 60 * 1000
              ? @"m\:ss\.f"
              : @"s\.f\s");

    private string GetThumbPath() {
      var fpc = MediaItem.FilePathCache;
      return $"{fpc[..fpc.LastIndexOf('.')]}_clip_{Id}.jpg";
    }

    public void SetMarker(bool start, int ms, double volume, double speed) {
      if (start) {
        TimeStart = ms;
        if (ms > TimeEnd)
          TimeEnd = 0;
      }
      else {
        if (ms < TimeStart) {
          TimeEnd = TimeStart;
          TimeStart = ms;
        }
        else
          TimeEnd = ms;
      }

      Volume = volume;
      Speed = speed;

      OnPropertyChanged(nameof(TimeStartStr));
      OnPropertyChanged(nameof(TimeEndStr));
      OnPropertyChanged(nameof(DurationStr));
      OnPropertyChanged(nameof(VolumeStr));
      OnPropertyChanged(nameof(SpeedStr));
    }
  }
}
