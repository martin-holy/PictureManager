using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PictureManager.Domain.Models {
  public class Faces : ITable {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|MediaItemId|PersonId|FaceBox|AvgHash
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[3].Split(',');
      var face = new Face(
        int.Parse(props[0]),
        int.Parse(props[2]),
        new Int32Rect(int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]), int.Parse(rect[3])),
        long.Parse(props[4])) { Csv = props };

      All.Add(face);
    }

    public void LinkReferences() {
      // AllDic are destroyed after load
      var mediaItems = Core.Instance.MediaItems.All.Cast<MediaItem>().ToDictionary(x => x.Id);
      var people = Core.Instance.People.All.Cast<Person>().ToDictionary(x => x.Id);

      foreach (var face in All.Cast<Face>()) {
        face.MediaItem = mediaItems[int.Parse(face.Csv[1])];

        if (face.PersonId > 0)
          face.Person = people[face.PersonId];

        // csv array is not needed any more
        face.Csv = null;
      }
    }
  }
}
