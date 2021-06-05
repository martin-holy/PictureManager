using System.Collections.Generic;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class Person : CatTreeViewItem, IRecord, ICatTreeViewTagItem {
    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();
    public List<VideoClip> VideoClips { get; set; }

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
