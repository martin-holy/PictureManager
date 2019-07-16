using System.Collections.Generic;
using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Keyword : BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public int Idx { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

    public string FullPath => GetFullPath();

    public Keyword(int id, string name, BaseTreeViewItem parent, int index) {
      Id = id;
      Title = name;
      Parent = parent;
      Idx = index;
      IconName = IconName.Tag;
    }

    public string ToCsv() {
      // ID|Name|Parent|Index|Children
      return string.Join("|",
        Id.ToString(),
        Title,
        (Parent as Keyword)?.Id.ToString(),
        Idx.ToString(),
        string.Join(",", Items.OfType<IRecord>().Select(x => x.Id)));
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

    public void Sort() {
      //TODO
      //BUG: asi bug, takhle to asi srovnavat nejde, kdyz dam move tak se prepisou indexy a tak "i" bude odkazovat na neco jineho
      /*var sorted = Items.Cast<Keyword>().OrderBy(x => x.Data.Idx).ThenBy(x => x.Title).ToList();
      for (var i = 0; i < Items.Count; i++) {
        Items.Move(Items.IndexOf(Items[i]), sorted.IndexOf((Keyword)Items[i]));
      }*/
    }

    public BaseMediaItem[] GetMediaItems(bool recursive) {
      return recursive ? GetMediaItemsRecursive() : MediaItems.ToArray();
    }

    public BaseMediaItem[] GetMediaItemsRecursive() {
      // get all Keywords
      var keywords = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref keywords);

      // get all MediaItems from keywords
      var mis = new List<BaseMediaItem>();
      foreach (var k in keywords)
        mis.AddRange(((Keyword) k).MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
