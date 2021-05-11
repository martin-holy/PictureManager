﻿using System;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class VideoClipViewModel: CatTreeViewBaseItem {

    public VideoClip Clip { get; }
    public string TimeStart => MediaItemClipsCategory.FormatTimeSpan(Clip.TimeStart);
    public string TimeEnd => MediaItemClipsCategory.FormatTimeSpan(Clip.TimeEnd);
    public string Duration => MediaItemClipsCategory.GetDuration(Clip.TimeStart, Clip.TimeEnd);
    public string Volume => $"{(int)(Clip.Volume * 100)}%";
    public string Speed => $"{Clip.Speed}x";
    public Uri ThumbPath => GetThumbPath();

    public VideoClipViewModel(VideoClip clip) {
      Clip = clip;
    }

    private Uri GetThumbPath() {
      var fpc = Clip.MediaItem.FilePathCache;
      return new Uri($"{fpc.Substring(0, fpc.LastIndexOf('.'))}_{Clip.Id}.jpg");
    }

    public void SetMarker(bool start, TimeSpan ts, double volume, double speed) {
      if (start) {
        Clip.TimeStart = (int)ts.TotalMilliseconds;
        if (ts.TotalMilliseconds > Clip.TimeEnd) Clip.TimeEnd = 0;
      }
      else {
        if (ts.TotalMilliseconds < Clip.TimeStart) {
          Clip.TimeEnd = Clip.TimeStart;
          Clip.TimeStart = (int)ts.TotalMilliseconds;
        }
        else {
          Clip.TimeEnd = (int)ts.TotalMilliseconds;
        }
      }

      Clip.Volume = volume;
      Clip.Speed = speed;

      OnPropertyChanged(nameof(TimeStart));
      OnPropertyChanged(nameof(TimeEnd));
      OnPropertyChanged(nameof(Duration));
      OnPropertyChanged(nameof(Volume));
      OnPropertyChanged(nameof(Speed));
      App.Core.Model.VideoClips.Helper.IsModified = true;
    }
  }
}
