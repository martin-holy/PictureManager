using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Keywords : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<Keyword> All { get; } = new List<Keyword>();
    public Dictionary<int, Keyword> AllDic { get; } = new Dictionary<int, Keyword>();

    private static readonly Mutex Mut = new Mutex();

    public Keywords() : base(Category.Keywords) {
      Title = "Keywords";
      IconName = IconName.TagLabel;
    }

    ~Keywords() {
      Mut.Dispose();
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void ClearBeforeLoad() {
      All.Clear();
      AllDic.Clear();
    }

    public void NewFromCsv(string csv) {
      // ID|Name|Parent|Index
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      AddRecord(new Keyword(id, props[1], null, int.Parse(props[3])) { Csv = props });
    }

    public void LinkReferences() {
      // ID|Name|Parent|Index
      // MediaItems to the Keyword are added in LinkReferences on MediaItem
      foreach (var keyword in All) {
        // reference to parent and back reference to children
        if (keyword.Csv[2] != string.Empty) {
          keyword.Parent = AllDic[int.Parse(keyword.Csv[2])];
          keyword.Parent.Items.Add(keyword);
        }

        // csv array is not needed any more
        keyword.Csv = null;
      }

      Items.Clear();
      LoadGroups();

      // add Keywords without group
      foreach (var keyword in All.Where(x => x.Parent == null)) {
        keyword.Parent = this;
        Items.Add(keyword);
      }

      // sort Keywords
      Sort(Items, true);
    }

    public static void Sort(ObservableCollection<BaseTreeViewItem> items, bool recursive) {
      var sorted = items.OfType<Keyword>().OrderBy(x => x.Idx).ThenBy(x => x.Title).ToList();
      var groupsCount = items.Count - sorted.Count;

      foreach (var k in sorted)
        items.Move(items.IndexOf(k), sorted.IndexOf(k) + groupsCount);

      if (!recursive) return;
      
      foreach (var item in items)
        Sort(item.Items, true);
    }

    private void AddRecord(Keyword record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public Keyword GetKeywordByFullPath(string fullPath) {
      if (fullPath.Equals(string.Empty)) return null;

      Mut.WaitOne();

      var pathNames = fullPath.Split('/');

      // get root Keyword => Parent is not Keyword but Keywords or CategoryGroup
      var keyword = All.SingleOrDefault(x => !(x.Parent is Keyword) && x.Title.Equals(pathNames[0]));

      // return Keyword if it was found and is 1 level type
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
      Mut.WaitOne();
      var keyword = new Keyword(Helper.GetNextId(), name, root, 0);

      // add new Keyword to the database and to the tree
      AddRecord(keyword);
      root.Items.Add(keyword);

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
        Sort(keyword.Parent.Items, false);
        Helper.IsModifed = true;
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
        foreach (var bmi in k.MediaItems) {
          bmi.Keywords.Remove(k);
          ACore.MediaItems.Helper.IsModifed = true;
        } 

        // remove Keyword from DB
        All.Remove(k);
        AllDic.Remove(k.Id);
      }

      Helper.IsModifed = true;
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
