using MH.UI.Interfaces;
using MH.Utils.Extensions;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoClipM : VideoItemM, IVideoClip {
  private int _timeEnd;
  private double _volume;
  private double _speed;

  public override string FilePathCache =>
    IOExtensions.PathCombine(Video.Folder.FullPathCache, FileNameCache(Video.FileName));
  public int TimeEnd { get => _timeEnd; set { _timeEnd = value; OnPropertyChanged(); OnPropertyChanged(nameof(Duration)); } }
  public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumePercent)); } }
  public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
  public int Duration => TimeEnd - TimeStart;
  public int VolumePercent => (int)(Volume * 100);

  public VideoClipM(int id, VideoM video, int timeStart) : base(id, video, timeStart) {
    PropertyChanged += (_, e) => {
      if (e.Is(nameof(TimeStart)))
        OnPropertyChanged(nameof(Duration));
    };
  }

  public override string FileNameCache(string fileName) =>
    $"{fileName[..fileName.LastIndexOf('.')]}_clip_{GetHashCode().ToString()}.jpg";
}