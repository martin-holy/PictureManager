using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace PictureManager.Data {
  public class Keywords: BaseItem {
    public ObservableCollection<Keyword> Items { get; set; }
    public DbStuff Db;

    public Keywords() {
      Items = new ObservableCollection<Keyword>();
    }

    public void Load() {
      Items.Clear();

      const string sql =
        "select Id, Keyword, (select count(PK.Id) from PictureKeyword as PK where PK.KeywordId in "
        + "(select XK.Id from Keywords as XK where XK.Keyword like K.Keyword||\"%\")) as PicturesCount, Idx from "
        + "Keywords as K order by Keyword";

      foreach (DataRow row in Db.Select(sql)) {
        Keyword newItem = new Keyword() {
          Id = (int) (long) row[0],
          Index = (int) (long) row[3],
          IconName = "appbar_tag",
          FullPath = (string) row[1]
        };

        if (!newItem.FullPath.Contains("/")) {
          newItem.Title = newItem.FullPath;
          Items.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('/') + 1);
          Keyword parentKeyword = GetKeywordByFullPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('/')), false);
          if (parentKeyword == null) continue;
          newItem.Parent = parentKeyword;
          parentKeyword.Items.Add(newItem);
        }
      }
    }

    public Keyword GetKeywordByFullPath(string fullPath, bool create) {
      Keyword parent = null;
      ObservableCollection<Keyword> root = Items;

      while (true) {
        if (root.Count == 0 || string.IsNullOrEmpty(fullPath)) return null;

        string[] keyParts = fullPath.Split('/');
        Keyword keyword = root.FirstOrDefault(k => k.Title.Equals(keyParts[0]));
        if (keyword == null) {
          if (!create) return null;
          keyword = CreateKeyword(root, parent, keyParts[0]);
        }
        if (keyParts.Length <= 1) return keyword;

        parent = keyword;
        root = keyword.Items;
        fullPath = fullPath.Substring(keyParts[0].Length + 1);
      }
    }

    public Keyword CreateKeyword(ObservableCollection<Keyword> root, Keyword parent, string name) {
      string kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      if (!Db.Execute($"insert into Keywords (Keyword) values ('{kFullPath}')")) return null;
      int keyId = Db.GetLastIdFor("Keywords");
      if (keyId == 0) return null;
      Keyword newKeyword = new Keyword {
        Id = keyId,
        IconName = "appbar_tag",
        FullPath = kFullPath,
        Title = name,
        Parent = parent
      };

      Keyword keyword =
        root.FirstOrDefault(k => k.Index == 0 && string.Compare(k.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Insert(keyword == null ? 0 : root.IndexOf(keyword), newKeyword);
      return newKeyword;
    }

    public void DeleteKeyword(Keyword keyword) {
      if (keyword.Items.Count != 0) return;
      Db.Execute($"delete from PictureKeyword where KeywordId = {keyword.Id}");
      Db.Execute($"delete from Keywords where Id = {keyword.Id}");
      var items = keyword.Parent == null ? Items : keyword.Parent.Items;
      items.Remove(keyword);
    }

  }
}
