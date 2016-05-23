using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Keywords: BaseTreeViewItem {
    public List<Keyword> AllKeywords; 
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

      foreach (Keyword newItem in ACore.Db.Keywords.OrderBy(x => x.Name).Select(x => new Keyword(x))) {
        var lioSlash = newItem.FullPath.LastIndexOf('/');
        if (lioSlash == -1) {
          newItem.Title = newItem.FullPath;
          newItem.Parent = this;
          Items.Add(newItem);
          AllKeywords.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(lioSlash + 1);
          Keyword parentKeyword = GetKeywordByFullPath(newItem.FullPath.Substring(0, lioSlash), false);
          if (parentKeyword == null) continue;
          newItem.Parent = parentKeyword;
          parentKeyword.Items.Add(newItem);
          AllKeywords.Add(newItem);
        }
      }

      AllKeywords.ForEach(x => x.Sort());
    }

    public void Sort() {
      var sorted = Items.Cast<Keyword>().OrderBy(x => x.Index).ThenBy(x => x.Title).ToList();
      for (var i = 0; i < Items.Count; i++) {
        Items.Move(Items.IndexOf(Items[i]), sorted.IndexOf((Keyword)Items[i]));
      }
    }

    public Keyword GetKeywordByFullPath(string fullPath, bool create) {
      if (create) Mut.WaitOne();

      var keyword = AllKeywords.SingleOrDefault(x => x.FullPath.Equals(fullPath));
      if (keyword != null) {
        if (create) Mut.ReleaseMutex();
        return keyword;
      }

      if (!create) return null;

      BaseTreeViewItem root = this;
      foreach (var keyPart in fullPath.Split('/')) {
        var k = root.Items.Cast<Keyword>().SingleOrDefault(x => x.Title.Equals(keyPart));
        root = k ?? CreateKeyword(root, keyPart);
      }

      Mut.ReleaseMutex();
      return root as Keyword;
    }

    public Keyword GetKeyword(int id) {
      return AllKeywords.SingleOrDefault(x => x.Id == id);
    }

    public void NewOrRename(BaseTreeViewItem item, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = ACore.WMain,
        IconName = "appbar_tag",
        Title = rename ? "Rename Keyword" : "New Keyword",
        Question = rename ? "Enter the new name for the keyword." : "Enter the name of the new keyword.",
        Answer = rename ? ((Keyword) item).Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, ((Keyword) item).Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        var root = rename ? item.Parent : item;
        if (root.Items.Cast<Keyword>().SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Keyword name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          var keyword = (Keyword) item;
          var path = keyword.FullPath;
          path = path.Contains("/")
            ? path.Substring(0, path.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1) + inputDialog.Answer
            : inputDialog.Answer;
          keyword.FullPath = path;
          keyword.Data.Name = path;
          keyword.Title = inputDialog.Answer;
          (keyword.Parent as Keywords)?.Sort();
          (keyword.Parent as Keyword)?.Sort();
          ACore.Db.Update(keyword.Data);
        } else CreateKeyword(item, inputDialog.Answer);
      }
    }

    public Keyword CreateKeyword(BaseTreeViewItem root, string name) {
      if (root == null) return null;

      var parent = root as Keyword;
      var dmKeyword = new DataModel.Keyword {
        Id = ACore.Db.GetNextIdFor<DataModel.Keyword>(),
        Name = parent == null ? name : $"{parent.FullPath}/{name}"
      };
      ACore.Db.Insert(dmKeyword);

      var vmKeyword = new Keyword(dmKeyword) {Parent = root};
      AllKeywords.Add(vmKeyword);

      Keyword keyword = root.Items.Cast<Keyword>().FirstOrDefault(k => k.Index == 0 && string.Compare(k.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Items.Insert(keyword == null ? 0 : root.Items.IndexOf(keyword), vmKeyword);

      return vmKeyword;
    }

    public void DeleteKeyword(Keyword keyword) {
      //TODO: SubmitChanges can submit other not commited changes as well!!
      foreach (var mik in ACore.Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Id)) {
        ACore.Db.DeleteOnSubmit(mik);
      }

      ACore.Db.DeleteOnSubmit(keyword.Data);
      ACore.Db.SubmitChanges();
      AllKeywords.Remove(keyword);

      var items = keyword.Parent == null ? Items : keyword.Parent.Items;
      items.Remove(keyword);
    }
  }
}
