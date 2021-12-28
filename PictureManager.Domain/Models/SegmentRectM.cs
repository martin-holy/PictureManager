﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Domain.Models {
  public sealed class SegmentRectM : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private double _scale;

    public int X {
      get => (int)((Segment.RotateTransformGetX(Segment.X) - Segment.Radius) * Scale);
      set {
        Segment.RotateTransformSetX((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Y {
      get => (int)((Segment.RotateTransformGetY(Segment.Y) - Segment.Radius) * Scale);
      set {
        Segment.RotateTransformSetY((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Size => (int)(Segment.Radius * 2 * Scale);

    public int Radius {
      get => (int)(Segment.Radius * Scale);
      set {
        Segment.Radius = (int)(value / Scale);
        OnPropertyChanged();
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
      }
    }

    public double Scale {
      get => _scale;
      set {
        _scale = value;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(Radius));
      }
    }

    public SegmentM Segment { get; set; }

    public SegmentRectM(SegmentM segment, double scale) {
      Segment = segment;
      Scale = scale;
    }
  }
}
