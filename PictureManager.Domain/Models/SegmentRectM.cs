using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class SegmentRectM : ObservableObject {
    private double _scale;

    public double Scale {
      get => _scale;
      set {
        _scale = value;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
      }
    }

    public double X => GetX(Segment) * Scale;
    public double Y => GetY(Segment) * Scale;
    public double Size => Segment.Size * Scale;
    public SegmentM Segment { get; set; }

    public SegmentRectM(SegmentM segment, double scale) {
      Segment = segment;
      Scale = scale;
    }

    private static double GetX(SegmentM s) =>
      (MediaOrientation)s.MediaItem.Orientation switch {
        MediaOrientation.Rotate90 => s.Y,
        MediaOrientation.Rotate180 => s.MediaItem.Width - s.X - s.Size,
        MediaOrientation.Rotate270 => s.MediaItem.Height - s.Y - s.Size,
        _ => s.X
      };

    private static double GetY(SegmentM s) =>
      (MediaOrientation)s.MediaItem.Orientation switch {
        MediaOrientation.Rotate90 => s.MediaItem.Width - s.X - s.Size,
        MediaOrientation.Rotate180 => s.MediaItem.Height - s.Y - s.Size,
        MediaOrientation.Rotate270 => s.X,
        _ => s.Y
      };
  }
}
