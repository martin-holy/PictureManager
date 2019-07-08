using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Keywords : VM.BaseCategoryItem, ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    private static readonly Mutex Mut = new Mutex();

    public Keywords() : base(Category.Keywords) {
      Title = "Keywords";
      IconName = IconName.TagLabel;
    }

    ~Keywords() {
      Mut.Dispose();
    }

    public void NewFromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Keyword(id, props[1], null, int.Parse(props[3])) { Csv = props });
    }

    public void LinkReferences(SimpleDB sdb) {
      foreach (var item in Records) {
        var keyword = (Keyword)item.Value;

        // reference to parent
        if (keyword.Csv[2] != string.Empty)
          keyword.Parent = (Keyword)Records[int.Parse(keyword.Csv[2])];

        // reference to childrens
        if (keyword.Csv[4] != string.Empty)
          foreach (var keywordId in keyword.Csv[4].Split(','))
            keyword.Items.Add((Keyword)Records[int.Parse(keywordId)]);

        // csv array is not needed any more
        keyword.Csv = null;
      }
    }

    public void Load() {
      //TODO
      /*Items.Clear();
      AllKeywords.Clear();

      LoadGroups();

      //Add top level Keywords in Group
      foreach (var g in Items.OfType<CategoryGroup>()) {
        foreach (var keyword in (from k in ACore.Db.Keywords
                                 join cgi in ACore.Db.CategoryGroupsItems
                                 on new { kid = k.Id, gid = g.Data.Id } equals new { kid = cgi.ItemId, gid = cgi.CategoryGroupId }
                                 select k).OrderBy(x => x.Idx).ThenBy(x => x.Name).Select(x => new Keyword(x) { Parent = g })) {
          g.Items.Add(keyword);
          AllKeywords.Add(keyword);
        }
      }

      //Add rest of Keywords
      foreach (var keyword in (from k in ACore.Db.Keywords where AllKeywords.All(ak => ak.Data.Id != k.Id) select k)
          .OrderBy(x => x.Name).Select(x => new Keyword(x))) {
        var lioSlash = keyword.Data.Name.LastIndexOf('/');
        if (lioSlash == -1) {
          keyword.Parent = this;
          Items.Add(keyword);
          AllKeywords.Add(keyword);
        }
        else {
          var parentKeyword = GetKeywordByFullPath(keyword.Data.Name.Substring(0, lioSlash), false);
          if (parentKeyword == null) continue;
          keyword.Parent = parentKeyword;
          parentKeyword.Items.Add(keyword);
          AllKeywords.Add(keyword);
        }
      }

      Sort();
      AllKeywords.ForEach(x => x.Sort());*/
    }

    public void Sort() {
      //TODO
      /*var sorted = Items.OfType<Keyword>().OrderBy(x => x.Data.Idx).ThenBy(x => x.Title).ToList();
      var groupsCount = Items.Count - sorted.Count;
      foreach (var k in sorted) {
        Items.Move(Items.IndexOf(k), sorted.IndexOf(k) + groupsCount);
      }*/
    }

    public Keyword GetKeyword(int id) {
      if (Records.TryGetValue(id, out var keyword))
        return (Keyword)keyword;
      return null;
    }

    public Keyword GetKeywordByFullPath(string fullPath, bool create) {
      return null;
      //TODO
      /*if (create) Mut.WaitOne();

      var keyword = AllKeywords.SingleOrDefault(x => x.Data.Name.Equals(fullPath));
      if (keyword != null) {
        if (create) Mut.ReleaseMutex();
        return keyword;
      }

      if (!create) return null;

      var ioSlash = fullPath.IndexOf('/');
      var keyFirstTitle = fullPath.Substring(0, ioSlash == -1 ? fullPath.Length : ioSlash);
      var root = AllKeywords.SingleOrDefault(x => x.Title.Equals(keyFirstTitle))?.Parent ??
                 (Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals("Auto Added")) ??
                  GroupCreate("Auto Added"));

      foreach (var keyPart in fullPath.Split('/')) {
        var k = root.Items.Cast<Keyword>().SingleOrDefault(x => x.Title.Equals(keyPart));
        root = k ?? CreateKeyword(root, keyPart);
      }

      Mut.ReleaseMutex();
      return root as Keyword;*/
    }

    public Keyword CreateKeyword(VM.BaseTreeViewItem root, string name) {
      // TODO keyword by mel mit parenta, ale ten muze bejt bud grupa, nebo keyword
      // TODO sort the tree
      Mut.WaitOne();
      var id = ACore.Sdb.Table<Keywords>().GetNextId();
      var keyword = new Keyword(id, name, null, 0);

      // add new Keyword to the database
      ACore.Sdb.Table<Keywords>().AddRecord(keyword);

      // add new Keyword to the tree
      if (root is CategoryGroup cg)
        cg.Items.Add(keyword);
      else
        Items.Add(keyword);

      Mut.ReleaseMutex();

      return keyword;
    }

    public override void ItemNewOrRename(VM.BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.TagLabel, "Keyword", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        var root = rename ? item.Parent : item;
        if (root.Items.SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("This keyword already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var keyword = (Keyword)item;
        keyword.Title = inputDialog.Answer;
        //TODO check the sorts
        (keyword.Parent as Keywords)?.Sort();
        (keyword.Parent as Keyword)?.Sort();
      }
      else CreateKeyword(item, inputDialog.Answer);
    }

    public override void ItemDelete(VM.BaseTreeViewItem item) {
      // TODO
      /*if (!(item is Keyword keyword)) return;
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();
      var keywords = AllKeywords.Where(x => x.Data.Name.StartsWith($"{keyword.Data.Name}/")).ToList();
      keywords.Add(keyword);

      foreach (var k in keywords) {
        foreach (var mik in ACore.Db.MediaItemKeywords.Where(x => x.KeywordId == k.Data.Id))
          DataModel.PmDataContext.DeleteOnSubmit(mik, lists);

        DataModel.PmDataContext.DeleteOnSubmit(k.Data, lists);
        AllKeywords.Remove(k);
      }

      item.Parent.Items.Remove(keyword);

      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(
            x => x.ItemId == keyword.Data.Id && x.CategoryGroupId == (item.Parent as CategoryGroup)?.Data.Id);
      if (cgi != null) {
        DataModel.PmDataContext.DeleteOnSubmit(cgi, lists);
      }

      ACore.Db.SubmitChanges(lists);*/
    }

    public void ItemMove(VM.BaseTreeViewTagItem item, VM.BaseTreeViewItem dest, bool dropOnTop) {
      //if (item.Parent == dest.Parent) => postun ve skupine, tzn. zmenit keyword.index
      //if (item.Parent != dest.Parent) => posun mezi skupinama, tzn. resetovat keyword.index a zaradit podle jmena
      /*if (dest is Keyword && item.Parent == dest.Parent) {
        var items = item.Parent.Items;
        var srcIndex = items.IndexOf(item);
        var destIndex = items.IndexOf(dest);
        var newIndex = srcIndex > destIndex ? (dropOnTop ? destIndex : destIndex + 1) : (dropOnTop ? destIndex - 1 : destIndex);

        items.Move(srcIndex, newIndex);

        var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();
        var i = 0;
        foreach (var itm in items.Where(x => x is Keyword).OfType<Keyword>()) {
          itm.Data.Idx = i;
          DataModel.PmDataContext.UpdateOnSubmit(itm.Data, lists);
          i++;
        }

        ACore.Db.SubmitChanges(lists);
      }
      else {
        var keyword = item as Keyword;
        if (keyword == null) return;
        var path = dest is Keyword ? $"{((Keyword)dest).Data.Name}/{keyword.Title}" : keyword.Title;

        keyword.Data.Idx = 0;
        keyword.Data.Name = path;
        ACore.Db.Update(keyword.Data);
        ItemMove(item, dest, ((Keyword)item).Data.Id);
      }*/
    }
  }
}
