using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PictureManager.ViewModel {
  public class Keywords: BaseCategoryItem {
    public List<Keyword> AllKeywords; 
    private static readonly Mutex Mut = new Mutex();

    public Keywords() : base(Categories.Keywords) {
      AllKeywords = new List<Keyword>();
      Title = "Keywords";
      IconName = "appbar_tag_label";
    }

    ~Keywords() {
      Mut.Dispose();
    }

    public void Load() {
      Items.Clear();
      AllKeywords.Clear();

      LoadGroups();

      //Add top level Keywords in Group
      foreach (var g in Items.OfType<CategoryGroup>()) {
        foreach (var keyword in (from k in ACore.Db.Keywords
                                 join cgi in ACore.Db.CategoryGroupsItems
                                 on new {kid = k.Id, gid = g.Id} equals new {kid = cgi.ItemId, gid = cgi.CategoryGroupId}
                                 select k).OrderBy(x => x.Idx).ThenBy(x => x.Name).Select(x => new Keyword(x) {Parent = g})) {
          g.Items.Add(keyword);
          AllKeywords.Add(keyword);
        }
      }

      //Add rest of Keywords
      foreach (var keyword in (from k in ACore.Db.Keywords where AllKeywords.All(ak => ak.Id != k.Id) select k)
          .OrderBy(x => x.Name).Select(x => new Keyword(x))) {
        var lioSlash = keyword.FullPath.LastIndexOf('/');
        if (lioSlash == -1) {
          keyword.Title = keyword.FullPath;
          keyword.Parent = this;
          Items.Add(keyword);
          AllKeywords.Add(keyword);
        } else {
          keyword.Title = keyword.FullPath.Substring(lioSlash + 1);
          Keyword parentKeyword = GetKeywordByFullPath(keyword.FullPath.Substring(0, lioSlash), false);
          if (parentKeyword == null) continue;
          keyword.Parent = parentKeyword;
          parentKeyword.Items.Add(keyword);
          AllKeywords.Add(keyword);
        }
      }

      Sort();
      AllKeywords.ForEach(x => x.Sort());
    }

    public void Sort() {
      var sorted = Items.OfType<Keyword>().OrderBy(x => x.Index).ThenBy(x => x.Title).ToList();
      var groupsCount = Items.Count - sorted.Count;
      foreach (var k in sorted) {
        Items.Move(Items.IndexOf(k), sorted.IndexOf(k) + groupsCount);
      }
    }

    public Keyword GetKeyword(int id) {
      return AllKeywords.SingleOrDefault(x => x.Id == id);
    }

    public Keyword GetKeywordByFullPath(string fullPath, bool create) {
      if (create) Mut.WaitOne();

      var keyword = AllKeywords.SingleOrDefault(x => x.FullPath.Equals(fullPath));
      if (keyword != null) {
        if (create) Mut.ReleaseMutex();
        return keyword;
      }

      if (!create) return null;

      var ioSlash = fullPath.IndexOf('/');
      var keyFirstTitle = fullPath.Substring(0, ioSlash == -1 ? fullPath.Length : ioSlash);
      BaseTreeViewItem root = AllKeywords.SingleOrDefault(x => x.Title.Equals(keyFirstTitle));
      if (root == null)
        root = Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals("Auto Added")) ?? GroupCreate("Auto Added");

      foreach (var keyPart in fullPath.Split('/')) {
        var k = root.Items.Cast<Keyword>().SingleOrDefault(x => x.Title.Equals(keyPart));
        root = k ?? CreateKeyword(root, keyPart);
      }

      Mut.ReleaseMutex();
      return root as Keyword;
    }

    public Keyword CreateKeyword(BaseTreeViewItem root, string name) {
      if (root == null) return null;

      var parent = root as Keyword;
      var dmKeyword = new DataModel.Keyword {
        Id = ACore.Db.GetNextIdFor<DataModel.Keyword>(),
        Name = parent == null ? name : $"{parent.FullPath}/{name}"
      };
      ACore.Db.Insert(dmKeyword);

      InsertCategoryGroupItem(root, dmKeyword.Id);

      var vmKeyword = new Keyword(dmKeyword) { Parent = root };
      AllKeywords.Add(vmKeyword);

      Mut.WaitOne();
      var keyword = root.Items.Cast<Keyword>().FirstOrDefault(k => k.Index == 0 && string.Compare(k.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Items.Insert(keyword == null ? 0 : root.Items.IndexOf(keyword), vmKeyword);
      Mut.ReleaseMutex();

      return vmKeyword;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, "appbar_tag", "Keyword", rename);

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          var keyword = (Keyword) item;
          var path = keyword.FullPath;
          path = path.Contains("/")
            ? path.Substring(0, path.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1) + inputDialog.Answer
            : inputDialog.Answer;
          keyword.FullPath = path;
          keyword.Title = inputDialog.Answer;
          (keyword.Parent as Keywords)?.Sort();
          (keyword.Parent as Keyword)?.Sort();
          ACore.Db.Update(keyword.Data);
        } else CreateKeyword(item, inputDialog.Answer);
      }
    }

    public override void ItemDelete(BaseTreeViewTagItem item) {
      var keyword = item as Keyword;
      if (keyword == null) return;
      var lists = ACore.Db.GetInsertUpdateDeleteLists();

      foreach (var mik in ACore.Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Id)) {
        ACore.Db.DeleteOnSubmit(mik, lists);
      }

      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(
            x => x.ItemId == item.Id && x.CategoryGroupId == (item.Parent as CategoryGroup)?.Id);
      if (cgi != null) {
        ACore.Db.DeleteOnSubmit(cgi, lists);
      }

      ACore.Db.DeleteOnSubmit(keyword.Data, lists);
      ACore.Db.SubmitChanges(lists);

      item.Parent.Items.Remove(keyword);
      AllKeywords.Remove(keyword); 
    }

    public void ItemMove(BaseTreeViewTagItem item, BaseTreeViewItem dest, bool dropOnTop) {
      //if (item.Parent == dest.Parent) => postun ve skupine, tzn. zmenit keyword.index
      //if (item.Parent != dest.Parent) => posun mezi skupinama, tzn. resetovat keyword.index a zaradit podle jmena
      if (dest is Keyword && item.Parent == dest.Parent) {
        var items = item.Parent.Items;
        var srcIndex = items.IndexOf(item);
        var destIndex = items.IndexOf(dest);
        var newIndex = srcIndex > destIndex ? (dropOnTop ? destIndex : destIndex + 1) : (dropOnTop ? destIndex - 1 : destIndex);

        items.Move(srcIndex, newIndex);

        var lists = ACore.Db.GetInsertUpdateDeleteLists();
        var i = 0;
        foreach (var itm in items.Where(x => x is Keyword).OfType<Keyword>()) {
          itm.Index = i;
          itm.Data.Idx = i;
          ACore.Db.UpdateOnSubmit(itm.Data, lists);
          i++;
        }

        ACore.Db.SubmitChanges(lists);
      } else {
        var keyword = item as Keyword;
        if (keyword == null) return;
        var path = dest is Keyword ? $"{((Keyword) dest).FullPath}/{keyword.Title}" : keyword.Title;

        keyword.Index = 0;
        keyword.Data.Idx = 0;
        keyword.FullPath = path;
        ACore.Db.Update(keyword.Data);
        ItemMove(item, dest);
      }
    }
  }
}
