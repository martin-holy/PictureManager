using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Keyword : CatTreeViewItem, IRecord, ICatTreeViewTagItem {
    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new();
    public List<VideoClip> VideoClips { get; set; }

    public string FullPath => GetFullPath();

    public Keyword(int id, string name, ICatTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;
      IconName = IconName.Tag;
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
      var keywords = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndItemsRecursive(this, ref keywords);

      // get all MediaItems from keywords
      var mis = new List<MediaItem>();
      foreach (var k in keywords.Cast<Keyword>())
        mis.AddRange(k.MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
