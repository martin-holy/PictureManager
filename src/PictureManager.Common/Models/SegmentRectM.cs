using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Models.MediaItems;

namespace PictureManager.Common.Models {
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
      s.MediaItem.Orientation.SwapRotateIf(s.MediaItem is not ImageM) switch {
        Orientation.Rotate90 => s.Y,
        Orientation.Rotate180 => s.MediaItem.Width - s.X - s.Size,
        Orientation.Rotate270 => s.MediaItem.Height - s.Y - s.Size,
        _ => s.X
      };

    private static double GetY(SegmentM s) =>
      s.MediaItem.Orientation.SwapRotateIf(s.MediaItem is not ImageM) switch {
        Orientation.Rotate90 => s.MediaItem.Width - s.X - s.Size,
        Orientation.Rotate180 => s.MediaItem.Height - s.Y - s.Size,
        Orientation.Rotate270 => s.X,
        _ => s.Y
      };
  }
}
