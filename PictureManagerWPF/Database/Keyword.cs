using System.Collections.Generic;
using System.Linq;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Keyword : VM.BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public int Idx { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

    public Keyword(int id, string name, Keyword parent, int index) {
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
        ((IRecord)Parent)?.Id.ToString(),
        Idx.ToString(),
        string.Join(",", Items.OfType<IRecord>().Select(x => x.Id)));
    }

    public string GetFullPath() {
      var parent = Parent;
      var names = new List<string> { Title };
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent;
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

    public void GetThisAndItems(ref List<Keyword> keywords) {
      keywords.Add(this);
      foreach (var item in Items) {
        ((Keyword)item).GetThisAndItems(ref keywords);
      }
    }

    public BaseMediaItem[] GetMediaItemsRecursive() {
      // get all Keywords
      var keywords = new List<Keyword>();
      GetThisAndItems(ref keywords);

      // get all MediaItems from keywords
      var mis = new List<BaseMediaItem>();
      foreach (var k in keywords)
        mis.AddRange(k.MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
