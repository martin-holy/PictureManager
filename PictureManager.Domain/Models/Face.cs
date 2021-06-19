using System.Drawing;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class Face: IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public Person Person { get; set; }
    public int PersonId { get; set; } // < 0 for unknown people, 0 for unknown, > 0 for known people
    public Rectangle FaceBox { get; set; }
    public long AvgHash { get; set; }

    public Face(int id, int personId, Rectangle faceBox, long avgHash) {
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
