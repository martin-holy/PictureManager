using SimpleDB;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PictureManager.Domain.Models {
  public class Face : ObservableObject, IRecord {
    private BitmapSource _picture;

    public string[] Csv { get; set; }
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public Person Person { get; set; }
    public int PersonId { get; set; } // < 0 for unknown people, 0 for unknown, > 0 for known people
    public Int32Rect FaceBox { get; set; }
    public long AvgHash { get; set; }
    public BitmapSource Picture { get => _picture; set { _picture = value; OnPropertyChanged(); } }

    public Face(int id, int personId, Int32Rect faceBox, long avgHash) {
      Id = id;
      PersonId = personId;
      FaceBox = faceBox;
      AvgHash = avgHash;
    }

    // ID|MediaItemId|PersonId|FaceBox|AvgHash
    public string ToCsv() {
      var faceBox = string.Join(",", FaceBox.X, FaceBox.Y, FaceBox.Width, FaceBox.Height);
      return string.Join("|", Id.ToString(), MediaItem.Id.ToString(), PersonId.ToString(), faceBox, AvgHash.ToString());
    }
  }
}
