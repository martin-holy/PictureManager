using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Domain.Models {
  public sealed class Face : ObservableObject, IRecord, IEquatable<Face> {
    private BitmapSource _picture;
    private bool _isSelected;

    #region DB Properties
    private Person _person;
    private int _personId;
    private int _x;
    private int _y;
    private int _size;

    public string[] Csv { get; set; }
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public Person Person { get => _person; set { _person = value; OnPropertyChanged(); } }

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
          var half = Size / 2;
          if (value < half) _x = half;
          if (value > MediaItem.Width - half) _x = MediaItem.Width - half;
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
          var half = Size / 2;
          if (value < half) _y = half;
          if (value > MediaItem.Height - half) _y = MediaItem.Height - half;
        }

        OnPropertyChanged();
      }
    }

    public int Size {
      get => _size;
      set {
        _size = value;

        // bounds check
        if (MediaItem != null) {
          var min = Math.Min(MediaItem.Width, MediaItem.Height);
          _size = value > min ? min : value;
          X = _x;
          Y = _y;
        }

        OnPropertyChanged();
      }
    }

    public int GroupId { get; set; } // 0 = not in the group of similar
    #endregion

    public BitmapSource Picture { get => _picture; set { _picture = value; OnPropertyChanged(); } }
    public Bitmap ComparePicture { get; set; }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsNotUnknown => PersonId != 0;
    public Dictionary<Face, double> Similar { get; set; }
    public double SimMax { get; set; }
    public string CacheFilePath => Extensions.PathCombine(Path.GetDirectoryName(MediaItem.FilePathCache), $"face_{Id}.jpg");

    public Face(int id, int personId, int x, int y, int size) {
      Id = id;
      PersonId = personId;
      X = x;
      Y = y;
      Size = size;
    }

    #region IEquatable implementation
    public bool Equals(Face other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as Face);
    public override int GetHashCode() => Id;
    public static bool operator ==(Face a, Face b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Face a, Face b) => !(a == b);
    #endregion

    // ID|MediaItemId|PersonId|GroupId|FaceBox
    public string ToCsv() =>
      string.Join("|", Id.ToString(), MediaItem.Id.ToString(), PersonId.ToString(), GroupId.ToString(), string.Join(",", X, Y, Size));

    public async Task SetPictureAsync(int size, bool reload = false) {
      if (reload) {
        Picture = null;
        ComparePicture = null;
      }

      Picture ??= await Task.Run(() => {
        var filePath = MediaItem.MediaType == MediaType.Image ? MediaItem.FilePath : MediaItem.FilePathCache;
        try {
          var cacheFilePath = CacheFilePath;
          if (!reload && File.Exists(cacheFilePath)) {
            return Imaging.GetBitmapSource(cacheFilePath);
          }
          else {
            var src = Imaging.GetCroppedBitmapSource(filePath, ToRect(), size);
            src.SaveAsJpg(80, cacheFilePath);
            return src;
          }
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

    public Int32Rect ToRect() {
      // if face is point
      if (Size == 0) {
        var x = X;
        var y = Y;
        if (x - 50 < 0) x = 50;
        if (y - 50 < 0) y = 50;
        if (x + 50 > MediaItem.Width) x = MediaItem.Width - 50;
        if (y + 50 > MediaItem.Height) y = MediaItem.Height - 50;
        return new Int32Rect(x - 50, y - 50, 100, 100);
      }

      var half = Size / 2;
      return new Int32Rect(X - half, Y - half, Size, Size);
    }

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
