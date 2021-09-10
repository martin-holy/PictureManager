using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Domain.Models {
  public sealed class Segment : ObservableObject, IRecord, IEquatable<Segment>, ISelectable {
    private BitmapSource _picture;
    private bool _isSelected;

    #region DB Properties
    private Person _person;
    private int _personId;
    private int _x;
    private int _y;
    private int _radius;

    public string[] Csv { get; set; }
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public Person Person { get => _person; set { _person = value; OnPropertyChanged(); } }
    public List<Keyword> Keywords { get; set; }

    public int PersonId { // < 0 for unknown people, 0 for unknown, > 0 for known people
      get => _personId;
      set {
        _personId = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsNotUnknown));
      }
    }

    public int X {
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

    public int Y {
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

    public int Radius {
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

    public BitmapSource Picture { get => _picture; set { _picture = value; OnPropertyChanged(); } }
    public Bitmap ComparePicture { get; set; }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsNotUnknown => PersonId != 0;
    public Dictionary<Segment, double> Similar { get; set; }
    public double SimMax { get; set; }
    public string CacheFilePath => Extensions.PathCombine(Path.GetDirectoryName(MediaItem.FilePathCache), $"segment_{Id}.jpg");

    public Segment(int id, int personId, int x, int y, int radius) {
      Id = id;
      PersonId = personId;
      X = x;
      Y = y;
      Radius = radius;
    }

    #region IEquatable implementation
    public bool Equals(Segment other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as Segment);
    public override int GetHashCode() => Id;
    public static bool operator ==(Segment a, Segment b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Segment a, Segment b) => !(a == b);
    #endregion IEquatable implementation

    // ID|MediaItemId|PersonId|SegmentBox|Keywords
    public string ToCsv() =>
      string.Join("|", Id.ToString(), MediaItem.Id.ToString(), PersonId.ToString(), string.Join(",", X, Y, Radius),
        Keywords == null ? string.Empty : string.Join(",", Keywords.Select(x => x.Id)));

    public async Task SetPictureAsync(int size, bool reload = false) {
      if (reload) {
        Picture = null;
        ComparePicture = null;
      }

      Picture ??= await Task.Run(() => {
        var filePath = MediaItem.MediaType == MediaType.Image ? MediaItem.FilePath : MediaItem.FilePathCache;
        try {
          var cacheFilePath = CacheFilePath;
          if (!reload && File.Exists(cacheFilePath))
            return Imaging.GetBitmapSource(cacheFilePath);

          var src = Imaging.GetCroppedBitmapSource(filePath, ToRect(), size);
          src.SaveAsJpg(80, cacheFilePath);

          return src;
        }
        catch (Exception ex) {
          Core.Instance.LogError(ex, filePath);
          return null;
        }
      });
    }

    public async Task SetComparePictureAsync(int size) {
      ComparePicture ??= await Task.Run(() => {
        try {
          return Picture?.ToGray().Resize(size).ToBitmap();
        }
        catch (Exception) {
          return null;
        }
      });
    }

    public Int32Rect ToRect() => new Int32Rect(X - Radius, Y - Radius, Radius * 2, Radius * 2);

    #region RotateTransform X, Y
    public int RotateTransformGetX(int x) =>
      MediaItem.Orientation switch {
        (int)MediaOrientation.Rotate90 => Y,
        (int)MediaOrientation.Rotate180 => MediaItem.Width - x,
        (int)MediaOrientation.Rotate270 => MediaItem.Height - Y,
        _ => x
      };

    public void RotateTransformSetX(int x) {
      switch (MediaItem.Orientation) {
        case (int)MediaOrientation.Rotate90: Y = x; break;
        case (int)MediaOrientation.Rotate180: X = MediaItem.Width - x; break;
        case (int)MediaOrientation.Rotate270: Y = MediaItem.Height - x; break;
        default: X = x; break;
      }
    }

    public int RotateTransformGetY(int y) =>
      MediaItem.Orientation switch {
        (int)MediaOrientation.Rotate90 => MediaItem.Width - X,
        (int)MediaOrientation.Rotate180 => MediaItem.Height - y,
        (int)MediaOrientation.Rotate270 => X,
        _ => y
      };

    public void RotateTransformSetY(int y) {
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
