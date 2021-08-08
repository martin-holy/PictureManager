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
  public class Face : ObservableObject, IRecord, IEquatable<Face> {
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
    public int X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public int Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public int Size { get => _size; set { _size = value; OnPropertyChanged(); } }
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

    public async Task SetPictureAsync(int size) {
      Picture ??= await Task.Run(() => {
        var filePath = MediaItem.MediaType == MediaType.Image ? MediaItem.FilePath : MediaItem.FilePathCache;
        try {
          var cacheFilePath = CacheFilePath;
          if (File.Exists(cacheFilePath)) {
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
      var half = Size / 2;
      return new Int32Rect(X - half, Y - half, Size, Size);
    }
  }
}
