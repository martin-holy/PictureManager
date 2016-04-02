using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Keywords: BaseTreeViewItem {
    public ObservableCollection<Keyword> Items { get; set; }
    public List<Keyword> AllKeywords; 
    public DataModel.PmDataContext Db;

    public Keywords() {
      Items = new ObservableCollection<Keyword>();
      AllKeywords = new List<Keyword>();
      Title = "Keywords";
      IconName = "appbar_tag";
    }

    public void Load() {
      Items.Clear();
      AllKeywords.Clear();

      foreach (Keyword newItem in Db.ListKeywords.OrderBy(x => x.Idx).ThenBy(x => x.Name).Select(x => new Keyword(x))) {
        if (!newItem.FullPath.Contains("/")) {
          newItem.Title = newItem.FullPath;
          Items.Add(newItem);
          AllKeywords.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('/') + 1);
          Keyword parentKeyword = GetKeywordByFullPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('/')), false);
          if (parentKeyword == null) continue;
          newItem.Parent = parentKeyword;
          parentKeyword.Items.Add(newItem);
          AllKeywords.Add(newItem);
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

    public Keyword GetKeyword(long id) {
      return AllKeywords.SingleOrDefault(x => x.Id == id);
    }

    public void NewOrRename(WMain wMain, ObservableCollection<Keyword> root, Keyword keyword, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
        IconName = "appbar_tag",
        Title = rename ? "Rename Keyword" : "New Keyword",
        Question = rename ? "Enter the new name for the keyword." : "Enter the name of the new keyword.",
        Answer = rename ? keyword.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, keyword.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (root.SingleOrDefault(x => x.FullPath.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Keyword name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          var path = keyword.FullPath;
          path = path.Contains("/")
            ? path.Substring(0, path.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1) + inputDialog.Answer
            : inputDialog.Answer;
          keyword.FullPath = path;
          keyword.Data.Name = path;
          keyword.Title = inputDialog.Answer;
          Db.SubmitChanges();
        }
        else CreateKeyword(root, keyword, inputDialog.Answer);
      }
    }

    public Keyword CreateKeyword(ObservableCollection<Keyword> root, Keyword parent, string name) {
      string kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      var dmKeyword = new DataModel.Keyword {
        Id = Db.GetNextIdFor("Keywords"),
        Name = kFullPath
      };

      Db.InsertOnSubmit(dmKeyword);
      Db.SubmitChanges();

      var vmKeyword = new Keyword(dmKeyword);

      Keyword keyword =
        root.FirstOrDefault(k => k.Index == 0 && string.Compare(k.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Insert(keyword == null ? 0 : root.IndexOf(keyword), vmKeyword);
      return vmKeyword;
    }

    public void DeleteKeyword(Keyword keyword) {
      foreach (var mik in Db.ListMediaItemKeywords.Where(x => x.KeywordId == keyword.Id)) {
        Db.DeleteOnSubmit(mik);
      }

      Db.DeleteOnSubmit(keyword.Data);
      Db.SubmitChanges();

      var items = keyword.Parent == null ? Items : keyword.Parent.Items;
      items.Remove(keyword);
    }
  }
}
