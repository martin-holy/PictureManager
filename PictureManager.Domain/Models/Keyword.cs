using System.Collections.Generic;
using System.Linq;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class Keyword : BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; }
    public int Idx { get; set; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();

    public string FullPath => GetFullPath();

    public Keyword(int id, string name, BaseTreeViewItem parent, int index) {
      Id = id;
      Title = name;
      Parent = parent;
      Idx = index;
      IconName = IconName.Tag;
    }

    public string ToCsv() {
      // ID|Name|Parent|Index
      return string.Join("|",
        Id.ToString(),
        Title,
        (Parent as Keyword)?.Id.ToString(),
        Idx.ToString());
    }

    private string GetFullPath() {
      var parent = Parent as Keyword;
      var names = new List<string> { Title };
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent as Keyword;
      }

      names.Reverse();

      return string.Join("/", names);
    }

    public MediaItem[] GetMediaItems(bool recursive) {
      return recursive ? GetMediaItemsRecursive() : MediaItems.ToArray();
    }

    public MediaItem[] GetMediaItemsRecursive() {
      // get all Keywords
      var keywords = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref keywords);

      // get all MediaItems from keywords
      var mis = new List<MediaItem>();
      foreach (var k in keywords.Cast<Keyword>())
        mis.AddRange(k.MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
