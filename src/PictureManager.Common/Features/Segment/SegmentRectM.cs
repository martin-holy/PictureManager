using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.Common.Features.Segment {
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

    public double X => _getX(Segment) * Scale;
    public double Y => _getY(Segment) * Scale;
    public double Size => Segment.Size * Scale;
    public SegmentM Segment { get; set; }

    public SegmentRectM(SegmentM segment, double scale) {
      Segment = segment;
      Scale = scale;
    }

    private static double _getX(SegmentM s) =>
      s.MediaItem.Orientation.SwapRotateIf(s.MediaItem is not ImageM) switch {
        Orientation.Rotate90 => s.Y,
        Orientation.Rotate180 => s.MediaItem.Width - s.X - s.Size,
        Orientation.Rotate270 => s.MediaItem.Height - s.Y - s.Size,
        _ => s.X
      };

    private static double _getY(SegmentM s) =>
      s.MediaItem.Orientation.SwapRotateIf(s.MediaItem is not ImageM) switch {
        Orientation.Rotate90 => s.MediaItem.Width - s.X - s.Size,
        Orientation.Rotate180 => s.MediaItem.Height - s.Y - s.Size,
        Orientation.Rotate270 => s.X,
        _ => s.Y
      };
  }
}
