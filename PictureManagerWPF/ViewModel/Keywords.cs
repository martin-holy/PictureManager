using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Keywords: BaseTreeViewItem {
    public List<Keyword> AllKeywords; 
    public DataModel.PmDataContext Db;
    private static readonly Mutex Mut = new Mutex();

    public Keywords() {
      AllKeywords = new List<Keyword>();
      Title = "Keywords";
      IconName = "appbar_tag";
    }

    ~Keywords() {
      Mut.Dispose();
    }

    public void Load() {
      Items.Clear();
      AllKeywords.Clear();

      foreach (Keyword newItem in Db.Keywords.OrderBy(x => x.Idx).ThenBy(x => x.Name).Select(x => new Keyword(x))) {
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
      if (create) Mut.WaitOne();

      Keyword parent = null;
      var root = Items;

      while (true) {
        if (string.IsNullOrEmpty(fullPath)) {
          if (create) Mut.ReleaseMutex();
          return null;
        }

        string[] keyParts = fullPath.Split('/');
        Keyword keyword = root.Cast<Keyword>().FirstOrDefault(k => k.Title.Equals(keyParts[0]));
        if (keyword == null) {
          if (!create) return null;
          
          keyword = CreateKeyword(root, parent, keyParts[0]);
        }
        if (keyParts.Length <= 1) {
          if (create) Mut.ReleaseMutex();
          return keyword;
        }

        parent = keyword;
        root = keyword.Items;
        fullPath = fullPath.Substring(keyParts[0].Length + 1);
      }
    }

    public Keyword GetKeyword(int id) {
      return AllKeywords.SingleOrDefault(x => x.Id == id);
    }

    public void NewOrRename(WMain wMain, ObservableCollection<BaseTreeViewItem> root, Keyword keyword, bool rename) {
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

        if (root.Cast<Keyword>().SingleOrDefault(x => x.FullPath.Equals(inputDialog.Answer)) != null) {
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
          Db.UpdateOnSubmit(keyword.Data);
          Db.SubmitChanges();
        }
        else CreateKeyword(root, keyword, inputDialog.Answer);
      }
    }

    public Keyword CreateKeyword(ObservableCollection<BaseTreeViewItem> root, Keyword parent, string name) {
      string kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      var dmKeyword = new DataModel.Keyword {
        Id = Db.GetNextIdFor<DataModel.Keyword>(),
        Name = kFullPath
      };

      Db.Insert(dmKeyword);

      var vmKeyword = new Keyword(dmKeyword);

      Keyword keyword =
        root.Cast<Keyword>().FirstOrDefault(k => k.Index == 0 && string.Compare(k.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Insert(keyword == null ? 0 : root.IndexOf(keyword), vmKeyword);
      return vmKeyword;
    }

    public void DeleteKeyword(Keyword keyword) {
      foreach (var mik in Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Id)) {
        Db.DeleteOnSubmit(mik);
      }

      Db.DeleteOnSubmit(keyword.Data);
      Db.SubmitChanges();

      var items = keyword.Parent == null ? Items : keyword.Parent.Items;
      items.Remove(keyword);
    }
  }
}
