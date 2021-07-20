using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Person : CatTreeViewItem, IRecord, ICatTreeViewTagItem {
    private Face _face;

    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();
    public List<VideoClip> VideoClips { get; set; }
    public List<Face> Faces { get; set; }
    public Face Face { get => _face; set { _face = value; OnPropertyChanged(); } }

    public Person(int id, string name) {
      Id = id;
      Title = name;
      IconName = IconName.People;
    }

    // ID|Name
    public string ToCsv() => string.Join("|",
      Id.ToString(),
      Title,
      Faces == null ? string.Empty : string.Join(",", Faces.Select(x => x.Id)));

    public MediaItem[] GetMediaItems() =>
      Core.Instance.Faces.All.Cast<Face>().Where(x => x.PersonId == Id).Select(x => x.MediaItem)
      .Concat(MediaItems).Distinct().OrderBy(x => x.FileName).ToArray();
  }
}
