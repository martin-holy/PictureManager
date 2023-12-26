using MH.UI.Interfaces;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoClipM : VideoItemM, IVideoClip {
  private int _timeEnd;
  private double _volume;
  private double _speed;

  public override string FilePathCache => GetFilePathCache();
  public int TimeEnd { get => _timeEnd; set { _timeEnd = value; OnPropertyChanged(); OnPropertyChanged(nameof(Duration)); } }
  public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumePercent)); } }
  public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
  public int Duration => TimeEnd - TimeStart;
  public int VolumePercent => (int)(Volume * 100);

  public VideoClipM(int id, VideoM video) : base(id, video) {
    PropertyChanged += (_, e) => {
      if (nameof(TimeStart).Equals(e.PropertyName))
        OnPropertyChanged(nameof(Duration));
    };
  }

  private string GetFilePathCache() {
    var fpc = Video.FilePathCache;
    return $"{fpc[..fpc.LastIndexOf('.')]}_clip_{GetHashCode().ToString()}.jpg";
  }
}