using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public class VideoClipViewModel: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public VideoClip Clip { get; }
    public string TimeStart => FormatTimeSpan(Clip.TimeStart);
    public string TimeEnd => FormatTimeSpan(Clip.TimeEnd);
    public Uri ThumbPath => GetThumbPath();
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public int Index { get => _index; set { _index = value; OnPropertyChanged(nameof(Name)); } }
    public string Name => string.IsNullOrEmpty(Clip.Name) ? $"Clip #{Index}" : Clip.Name;
    public string Duration => GetDuration();
    public string Volume => $"{(int)(Clip.Volume * 100)}%";
    public string Speed => $"{Clip.Speed}x";

    private bool _isSelected;
    private int _index;

    public VideoClipViewModel(VideoClip clip) {
      Clip = clip;
    }

    private string GetDuration() {
      if (Clip.TimeEnd == 0) return string.Empty;

      string format;
      var ms = Clip.TimeEnd - Clip.TimeStart;
      if (ms >= 60 * 60 * 1000) format = @"h\:mm\:ss\.f";
      else if (ms >= 60 * 1000) format = @"m\:ss\.f";
      else format = @"s\.f\s";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    private string FormatTimeSpan(int ms) {
      if (ms == 0) return string.Empty;

      var format = ms >= 60 * 60 * 1000 ? @"h\:mm\:ss\.fff" : @"m\:ss\.fff";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    private Uri GetThumbPath() {
      var fpc = Clip.MediaItem.FilePathCache;
      return new System.Uri($"{fpc.Substring(0, fpc.LastIndexOf('.'))}_{Clip.Id}.jpg");
    }

    public void CreateThumbnail(FrameworkElement visual, bool reCreate = false) {
      if (!File.Exists(ThumbPath.LocalPath) || reCreate) {
        Utils.Imaging.CreateVideoThumbnailFromVisual(visual, ThumbPath.LocalPath, Settings.Default.ThumbnailSize);

        OnPropertyChanged(nameof(ThumbPath));
      }
    }

    public void SetMarker(bool start, TimeSpan ts, double volume, double speed) {
      if (start) {
        Clip.TimeStart = (int) ts.TotalMilliseconds;
        if (ts.TotalMilliseconds > Clip.TimeEnd) Clip.TimeEnd = 0;
      }
      else {
        if (ts.TotalMilliseconds < Clip.TimeStart) {
          Clip.TimeEnd = Clip.TimeStart;
          Clip.TimeStart = (int) ts.TotalMilliseconds;
        }
        else {
          Clip.TimeEnd = (int) ts.TotalMilliseconds;
        }
      }

      Clip.Volume = volume;
      Clip.Speed = speed;

      OnPropertyChanged(nameof(TimeStart));
      OnPropertyChanged(nameof(TimeEnd));
      OnPropertyChanged(nameof(Duration));
      OnPropertyChanged(nameof(Volume));
      OnPropertyChanged(nameof(Speed));
      Core.Instance.VideoClips.Helper.IsModified = true;
    }

    public void RenameClip(string newName) {
      Clip.Name = newName;
      OnPropertyChanged(nameof(Name));
      Core.Instance.VideoClips.Helper.IsModified = true;
    }
  }
}
