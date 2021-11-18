using System;
using System.Collections.Generic;
using MH.Utils.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipM : ObservableObject, IRecord {
    private string _name;
    private int _timeStart;
    private int _timeEnd;
    private double _volume;
    private double _speed;

    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public MediaItemM MediaItem { get; set; }
    public int TimeStart { get => _timeStart; set { _timeStart = value; OnPropertyChanged(); } }
    public int TimeEnd { get => _timeEnd; set { _timeEnd = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); } }
    public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
    public int? Rating { get; set; }
    public string Comment { get; set; }
    public List<PersonM> People { get; set; }
    public List<KeywordM> Keywords { get; set; }
    public VideoClipsGroupM Group { get; set; }

    public string TimeStartStr => FormatTimeSpan(TimeStart);
    public string TimeEndStr => FormatTimeSpan(TimeEnd);
    public string DurationStr => GetDuration(TimeStart, TimeEnd);
    public string VolumeStr => $"{(int)(Volume * 100)}%";
    public string SpeedStr => $"{Speed}x";
    public string ThumbPath => GetThumbPath();

    public VideoClipM(int id, MediaItemM mediaItem) {
      Id = id;
      MediaItem = mediaItem;
    }

    private static string FormatTimeSpan(int ms) {
      var format = ms >= 60 * 60 * 1000 ? @"h\:mm\:ss\.fff" : @"m\:ss\.fff";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    private static string GetDuration(int start, int end) {
      if (end == 0) return string.Empty;

      string format;
      var ms = end - start;
      if (ms >= 60 * 60 * 1000) format = @"h\:mm\:ss\.f";
      else if (ms >= 60 * 1000) format = @"m\:ss\.f";
      else format = @"s\.f\s";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    private string GetThumbPath() {
      var fpc = MediaItem.FilePathCache;
      return $"{fpc[..fpc.LastIndexOf('.')]}_{Id}.jpg";
    }

    public void SetMarker(bool start, int ms, double volume, double speed) {
      if (start) {
        TimeStart = ms;
        if (ms > TimeEnd) TimeEnd = 0;
      }
      else {
        if (ms < TimeStart) {
          TimeEnd = TimeStart;
          TimeStart = ms;
        }
        else {
          TimeEnd = ms;
        }
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
