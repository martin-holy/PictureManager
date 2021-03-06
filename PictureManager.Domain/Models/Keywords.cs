﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class Keywords : BaseCategoryItem, ITable, ICategoryItem {
    public TableHelper Helper { get; set; }
    public List<Keyword> All { get; } = new List<Keyword>();
    public Dictionary<int, Keyword> AllDic { get; } = new Dictionary<int, Keyword>();

    private static readonly Mutex Mut = new Mutex();
    private CategoryGroup _autoAddedGroup;

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

    public void LoadFromFile() {
      All.Clear();
      AllDic.Clear();
      Helper.LoadFromFile();
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
        if (!string.IsNullOrEmpty(keyword.Csv[2])) {
          keyword.Parent = AllDic[int.Parse(keyword.Csv[2])];
          keyword.Parent.Items.Add(keyword);
        }

        // csv array is not needed any more
        keyword.Csv = null;
      }

      Items.Clear();
      LoadGroups();

      // group for keywords automatically added from MediaItems metadata
      _autoAddedGroup = Items.OfType<CategoryGroup>().SingleOrDefault(x => x.Title.Equals("Auto Added")) ??
                       GroupCreate("Auto Added");

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

      for (var i = 0; i < sorted.Count; i++)
        items.Move(items.IndexOf(sorted[i]), i + groupsCount);

      if (!recursive) return;
      
      foreach (var item in items)
        Sort(item.Items, true);
    }

    private void AddRecord(Keyword record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public Keyword GetByFullPath(string fullPath) {
      if (string.IsNullOrEmpty(fullPath)) return null;

      Mut.WaitOne();

      var pathNames = fullPath.Split('/');

      // get top level Keyword => Parent is not Keyword but Keywords or CategoryGroup
      var keyword = All.SingleOrDefault(x => !(x.Parent is Keyword) && x.Title.Equals(pathNames[0]));

      // return Keyword if it was found and is 1 level type
      if (keyword != null && pathNames.Length == 1) {
        Mut.ReleaseMutex();
        return keyword;
      }

      // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
      var root = keyword?.Parent ?? _autoAddedGroup;

      // for each keyword in pathNames => find or create
      foreach (var name in pathNames) {
        root = root.Items.OfType<Keyword>().SingleOrDefault(x => x.Title.Equals(name))
               ?? CreateKeyword(root, name);
      }

      Mut.ReleaseMutex();
      return root as Keyword;
    }

    private Keyword CreateKeyword(BaseTreeViewItem root, string name) {
      Mut.WaitOne();
      var keyword = new Keyword(Helper.GetNextId(), name, root, 0);

      // add new Keyword to the database and to the tree
      AddRecord(keyword);
      ItemSetInPlace(root, true, keyword);

      if (root is CategoryGroup)
        Core.Instance.CategoryGroups.Helper.IsModified = true;

      Mut.ReleaseMutex();

      return keyword;
    }

    public string ValidateNewItemTitle(BaseTreeViewItem root, string name) {
      return root.Items.SingleOrDefault(x => x.Title.Equals(name)) != null
        ? $"{name} keyword already exists!"
        : null;
    }

    public void ItemCreate(BaseTreeViewItem root, string name) {
      CreateKeyword(root, name);
      Core.Instance.Sdb.SaveAllTables();
    }

    public void ItemRename(BaseTreeViewItem item, string name) {
      item.Title = name;
      Sort(item.Parent.Items, false);
      SaveToFile();
    }

    public void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Keyword keyword)) return;

      // remove Keyword from the tree
      keyword.Parent.Items.Remove(keyword);

      // remove Keyword from the group
      if (keyword.Parent is CategoryGroup group) {
        group.Items.Remove(keyword);
        Core.Instance.CategoryGroups.Helper.IsModified = true;
      }

      // get all descending keywords
      var keywords = new List<BaseTreeViewItem>();
      keyword.GetThisAndItemsRecursive(ref keywords);

      foreach (var k in keywords.Cast<Keyword>()) {
        // remove Keyword from MediaItems
        if (k.MediaItems.Count > 0) {
          foreach (var mi in k.MediaItems) {
            mi.Keywords.Remove(k);
            if (mi.Keywords.Count == 0)
              mi.Keywords = null;
          }
          Core.Instance.MediaItems.Helper.IsModified = true;
        }

        k.Parent = null;

        // remove Keyword from DB
        All.Remove(k);
        AllDic.Remove(k.Id);
      }

      Helper.IsModified = true;
    }

    public void ItemMove(BaseTreeViewTagItem item, BaseTreeViewItem dest, bool dropOnTop) {
      // move in a group
      if (dest is Keyword && item.Parent == dest.Parent) {
        var items = item.Parent.Items;
        var srcIndex = items.IndexOf(item);
        var destIndex = items.IndexOf(dest);
        var newIndex = srcIndex > destIndex ? (dropOnTop ? destIndex : destIndex + 1) : (dropOnTop ? destIndex - 1 : destIndex);

        items.Move(srcIndex, newIndex);

        var i = 0;
        foreach (var itm in items.OfType<Keyword>()) itm.Idx = i++;
      }
      // move between groups
      else { 
        if (!(item is Keyword keyword)) return;

        keyword.Idx = 0;
        ItemMove(item, dest);
      }

      Helper.IsModified = true;
    }
  }
}
