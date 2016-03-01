using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.Data {
  public class Keywords: BaseItem {
    public ObservableCollection<Keyword> Items { get; set; }
    public DbStuff Db;

    public Keywords() {
      Items = new ObservableCollection<Keyword>();
    }

    public void Load() {
      Items.Clear();

      foreach (DataRow row in Db.Select("select Id, Keyword, Idx from Keywords order by Idx, Keyword")) {
        Keyword newItem = new Keyword {
          Id = (int) (long) row[0],
          Index = (int) (long) row[2],
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
        if (string.IsNullOrEmpty(fullPath)) return null;

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

    public void NewOrRename(WMain wMain, ObservableCollection<Keyword> root, Keyword keyword, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
        IconName = "appbar_tag",
        Title = rename ? "Rename Keyword" : "New Keyword",
        Question = rename ? "Enter new name for keyword." : "Enter name of new keyword.",
        Answer = rename ? keyword.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) keyword.Rename(Db, inputDialog.Answer);
        else CreateKeyword(root, keyword, inputDialog.Answer);
      }
    }

    public Keyword CreateKeyword(ObservableCollection<Keyword> root, Keyword parent, string name) {
      string kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      if (!Db.Execute($"insert into Keywords (Keyword) values ('{kFullPath}')")) return null;
      var keyId = Db.GetLastIdFor("Keywords");
      if (keyId == null) return null;
      Keyword newKeyword = new Keyword {
        Id = (int) keyId,
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
      Db.Execute($"delete from MediaItemKeyword where KeywordId = {keyword.Id}");
      Db.Execute($"delete from Keywords where Id = {keyword.Id}");
      var items = keyword.Parent == null ? Items : keyword.Parent.Items;
      items.Remove(keyword);
    }

  }
}
