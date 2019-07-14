using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Keywords : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
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
      // ID|Name|Parent|Index|Children
      var props = csv.Split('|');
      if (props.Length != 5) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Keyword(id, props[1], null, int.Parse(props[3])) { Csv = props });
    }

    public void LinkReferences(SimpleDB sdb) {
      // ID|Name|Parent|Index|Children
      // MediaItems to the Keyword are added in LinkReferences on MediaItem
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

      Items.Clear();
      LoadGroups();

      // add Keywords without group
      foreach (var keyword in Records.Values.Cast<Keyword>().Where(x => x.Parent == null)) {
        keyword.Parent = this;
        Items.Add(keyword);
      }

      //TODO Sort
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

    public Keyword GetKeywordByFullPath(string fullPath) {
      if (fullPath.Equals(string.Empty)) return null;

      Mut.WaitOne();

      var pathNames = fullPath.Split('/');
      var title = pathNames[0];
      var keyword = Records.Values.Cast<Keyword>().SingleOrDefault(x => !(x.Parent is Keyword) && x.Title.Equals(title));

      if (keyword != null && pathNames.Length == 1) {
        Mut.ReleaseMutex();
        return keyword;
      }

      // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
      var root = keyword?.Parent ??
                 (Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals("Auto Added")) ??
                  GroupCreate("Auto Added"));

      foreach (var name in pathNames) {
        root = root.Items.OfType<Keyword>().SingleOrDefault(x => x.Title.Equals(name))
               ?? CreateKeyword(root, name);
      }

      Mut.ReleaseMutex();
      return root as Keyword;
    }

    public Keyword CreateKeyword(BaseTreeViewItem root, string name) {
      // TODO keyword by mel mit parenta, ale ten muze bejt bud grupa, nebo keyword
      // TODO sort the tree
      Mut.WaitOne();
      var id = ACore.Keywords.Helper.GetNextId();
      var keyword = new Keyword(id, name, null, 0);

      // add new Keyword to the database
      ACore.Keywords.Helper.AddRecord(keyword);

      // add new Keyword to the tree
      (root is CategoryGroup cg ? cg.Items : Items).Add(keyword);

      Mut.ReleaseMutex();

      return keyword;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
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

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Keyword keyword)) return;

      // remove Keyword from the tree
      keyword.Parent.Items.Remove(keyword);

      // get all descending keywords
      var keywords = new List<BaseTreeViewItem>();
      keyword.GetThisAndItemsRecursive(ref keywords);

      foreach (var k in keywords.Cast<Keyword>()) {
        // remove Keyword from MediaItems
        foreach (var bmi in k.MediaItems)
          bmi.Keywords.Remove(k);

        // remove Keyword from DB
        Records.Remove(k.Id);
      }
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

        var i = 0;
        foreach (var itm in items.OfType<Keyword>()) itm.Idx = i++;
      }
      else {
        if (!(item is Keyword keyword)) return;

        keyword.Idx = 0;
        ItemMove(item, dest, ((Keyword)item).Id);
      }
    }
  }
}
