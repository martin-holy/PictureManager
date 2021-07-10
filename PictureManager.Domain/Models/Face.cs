using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Domain.Models {
  public class Face : ObservableObject, IRecord, IEquatable<Face> {
    private BitmapSource _picture;
    private bool _isSelected;

    #region DB Properties
    private int _personId;

    public string[] Csv { get; set; }
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public Person Person { get; set; }
    public int PersonId { // < 0 for unknown people, 0 for unknown, > 0 for known people
      get => _personId;
      set {
        _personId = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsUnknown));
      }
    }
    public Int32Rect FaceBox { get; set; }
    public List<Face> NotSimilar { get; set; }
    #endregion

    #region temp
    private double _sim;
    public double Sim { get => _sim; set { _sim = value; OnPropertyChanged(); } }
    #endregion

    public BitmapSource Picture { get => _picture; set { _picture = value; OnPropertyChanged(); } }
    public Bitmap ComparePicture { get; set; }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsUnknown => PersonId == 0;
    public List<(Face face, double similarity)> Similar { get; set; }

    public Face(int id, int personId, Int32Rect faceBox) {
      Id = id;
      PersonId = personId;
      FaceBox = faceBox;
    }

    #region IEquatable implementation
    public bool Equals(Face other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as Face);
    public override int GetHashCode() => Id;
    public static bool operator ==(Face a, Face b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Face a, Face b) => !(a == b);
    #endregion

    // ID|MediaItemId|PersonId|FaceBox|NotSimilar
    public string ToCsv() {
      var faceBox = string.Join(",", FaceBox.X, FaceBox.Y, FaceBox.Width, FaceBox.Height);
      return string.Join("|", Id.ToString(), MediaItem.Id.ToString(), PersonId.ToString(), faceBox,
        NotSimilar == null ? string.Empty : string.Join(",", NotSimilar.Select(x => x.Id)));
    }

    public async Task SetPictureAsync(int size) {
      Picture ??= await Task.Run(() => {
        var filePath = MediaItem.MediaType == MediaType.Image ? MediaItem.FilePath : MediaItem.FilePathCache;
        return Imaging.GetCroppedBitmapSource(filePath, FaceBox, size);
      });
    }

    public async Task SetComparePictureAsync(int size) {
      ComparePicture ??= await Task.Run(() => {
        return Picture.ToGray().Resize(size).ToBitmap();
      });
    }
  }
}
