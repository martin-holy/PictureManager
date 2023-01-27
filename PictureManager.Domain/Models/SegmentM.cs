using System;
using System.Collections.Generic;
using System.IO;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class SegmentM : ObservableObject, IEquatable<SegmentM>, ISelectable, IRecord {
    private bool _isSelected;

    #region DB Properties
    private PersonM _person;
    private double _x;
    private double _y;
    private double _radius;

    public int Id { get; }
    public MediaItemM MediaItem { get; set; }
    public PersonM Person { get => _person; set { _person = value; OnPropertyChanged(); } }
    public List<KeywordM> Keywords { get; set; }

    public double X {
      get => _x;
      set {
        _x = value;

        // bounds check
        if (MediaItem != null) {
          if (value < Radius) _x = Radius;
          if (value > MediaItem.Width - Radius) _x = MediaItem.Width - Radius;
        }

        OnPropertyChanged();
      }
    }

    public double Y {
      get => _y;
      set {
        _y = value;

        // bounds check
        if (MediaItem != null) {
          if (value < Radius) _y = Radius;
          if (value > MediaItem.Height - Radius) _y = MediaItem.Height - Radius;
        }

        OnPropertyChanged();
      }
    }

    public double Radius {
      get => _radius;
      set {
        _radius = value;

        // bounds check
        if (MediaItem != null) {
          var min = Math.Min(MediaItem.Width, MediaItem.Height) / 2;
          _radius = value > min ? min : value;
          X = _x;
          Y = _y;
        }

        OnPropertyChanged();
      }
    }
    #endregion DB Properties

    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public Dictionary<SegmentM, double> Similar { get; set; }
    public double SimMax { get; set; }
    public string FilePathCache => IOExtensions.PathCombine(Path.GetDirectoryName(MediaItem.FilePathCache), $"segment_{Id}.jpg");

    public SegmentM() { }

    public SegmentM(int id, double x, double y, double radius) {
      Id = id;
      X = x;
      Y = y;
      Radius = radius;
    }

    #region IEquatable implementation
    public bool Equals(SegmentM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as SegmentM);
    public override int GetHashCode() => Id;
    public static bool operator ==(SegmentM a, SegmentM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(SegmentM a, SegmentM b) => !(a == b);
    #endregion IEquatable implementation

    #region RotateTransform X, Y
    public double RotateTransformGetX(double x) =>
      MediaItem.Orientation switch {
        (int)MediaOrientation.Rotate90 => Y,
        (int)MediaOrientation.Rotate180 => MediaItem.Width - x,
        (int)MediaOrientation.Rotate270 => MediaItem.Height - Y,
        _ => x
      };

    public void RotateTransformSetX(double x) {
      switch (MediaItem.Orientation) {
        case (int)MediaOrientation.Rotate90: Y = x; break;
        case (int)MediaOrientation.Rotate180: X = MediaItem.Width - x; break;
        case (int)MediaOrientation.Rotate270: Y = MediaItem.Height - x; break;
        default: X = x; break;
      }
    }

    public double RotateTransformGetY(double y) =>
      MediaItem.Orientation switch {
        (int)MediaOrientation.Rotate90 => MediaItem.Width - X,
        (int)MediaOrientation.Rotate180 => MediaItem.Height - y,
        (int)MediaOrientation.Rotate270 => X,
        _ => y
      };

    public void RotateTransformSetY(double y) {
      switch (MediaItem.Orientation) {
        case (int)MediaOrientation.Rotate90: X = MediaItem.Width - y; break;
        case (int)MediaOrientation.Rotate180: Y = MediaItem.Height - y; break;
        case (int)MediaOrientation.Rotate270: X = y; break;
        default: Y = y; break;
      }
    }
    #endregion RotateTransform X, Y
  }
}
