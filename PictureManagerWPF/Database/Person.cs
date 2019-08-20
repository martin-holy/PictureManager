using System.Collections.Generic;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Person : BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public List<MediaItem> MediaItems { get; set; } = new List<MediaItem>();

    public Person(int id, string name) {
      Id = id;
      Title = name;
      IconName = IconName.People;
    }

    public string ToCsv() {
      // ID|Name
      return string.Join("|", Id.ToString(), Title);
    }
  }
}
