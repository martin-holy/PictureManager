using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Keywords : BaseCatTreeViewCategory, ITable {
    private readonly Core _core;
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Keyword> AllDic { get; set; }
    public ICatTreeViewGroup AutoAddedGroup { get; set; }

    public Keywords(Core core) : base(Category.Keywords) {
      _core = core;
      DataAdapter = new KeywordsDataAdapter(core, this);
      Title = "Keywords";
      IconName = IconName.TagLabel;
      CanHaveGroups = true;
      CanHaveSubItems = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public Keyword GetByFullPath(string fullPath) {
      if (string.IsNullOrEmpty(fullPath)) return null;

      return _core.RunOnUiThread(() => {
        var pathNames = fullPath.Split('/');

        // get top level Keyword => Parent is not Keyword but Keywords or CategoryGroup
        var keyword = All.Cast<Keyword>().SingleOrDefault(x => !(x.Parent is Keyword) && x.Title.Equals(pathNames[0]));

        // return Keyword if it was found and is 1 level type
        if (keyword != null && pathNames.Length == 1)
          return keyword;

        // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
        var root = keyword?.Parent ?? AutoAddedGroup;

        // for each keyword in pathNames => find or create
        foreach (var name in pathNames)
          root = root.Items.OfType<Keyword>().SingleOrDefault(x => x.Title.Equals(name)) ?? ItemCreate(root, name);

        return root as Keyword;
      }).Result;
    }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new Keyword(DataAdapter.GetNextId(), name, root);
      var idx = CatTreeViewUtils.SetItemInPlace(root, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, root, idx);

      All.Insert(allIdx, item);
      DataAdapter.IsModified = true;
      if (root is ICatTreeViewGroup)
        _core.CategoryGroups.DataAdapter.IsModified = true;

      return item;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not Keyword keyword) return;

      // remove Keyword from the tree
      item.Parent.Items.Remove(item);

      if (item.Parent is CategoryGroup)
        _core.CategoryGroups.DataAdapter.IsModified = true;

      // get all descending keywords
      var keywords = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndItemsRecursive(keyword, ref keywords);

      // remove Keywords from People
      foreach (var person in _core.People.All.Cast<Person>().Where(p => p.Keywords != null && p.Keywords.Any(k => keywords.Contains(k))))
        foreach (var k in keywords.Cast<Keyword>())
          if (person.Keywords.Remove(k)) {
            if (!person.Keywords.Any())
              person.Keywords = null;
            person.UpdateDisplayKeywords();
            _core.People.DataAdapter.IsModified = true;
          }

      foreach (var k in keywords.Cast<Keyword>()) {
        // remove Keyword from MediaItems
        if (k.MediaItems.Count > 0) {
          foreach (var mi in k.MediaItems) {
            mi.Keywords.Remove(k);
            if (mi.Keywords.Count == 0)
              mi.Keywords = null;
          }
          _core.MediaItems.DataAdapter.IsModified = true;
        }

        k.Parent = null;

        // remove Keyword from DB
        All.Remove(k);
        DataAdapter.IsModified = true;
      }
    }

    /// <summary>
    /// Toggle Keyword in the List
    /// </summary>
    /// <param name="k">Keyword</param>
    /// <param name="list">List</param>
    /// <param name="onAdd">Action on Add</param>
    /// <param name="onRemove">Action on Remove</param>
    public static void Toggle(Keyword k, ref List<Keyword> list, Action onAdd, Action<Keyword> onRemove) {
      list ??= new();

      var allKeywords = new List<ICatTreeViewItem>();
      foreach (var keyword in list)
        CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref allKeywords);

      if (allKeywords.OfType<Keyword>().Any(x => x.Id.Equals(k.Id))) {
        if (list.Remove(k))
          onRemove?.Invoke(k);
        if (list.Count == 0)
          list = null;
      }
      else {
        // remove possible redundant keywords 
        // example: if new keyword is "Weather/Sunny" keyword "Weather" is redundant
        var newKeywords = new List<ICatTreeViewItem>();
        CatTreeViewUtils.GetThisAndParentRecursive(k, ref newKeywords);
        foreach (var newKeyword in newKeywords.OfType<Keyword>())
          if (list.Remove(newKeyword))
            onRemove?.Invoke(newKeyword);

        list.Add(k);
        onAdd?.Invoke();
      }
    }

    public static List<Keyword> GetAllKeywords(List<Keyword> keywords) {
      var outKeywords = new List<Keyword>();
      if (keywords == null) return outKeywords;
      var allKeywords = new List<ICatTreeViewItem>();

      foreach (var keyword in keywords)
        CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref allKeywords);

      foreach (var keyword in allKeywords.OfType<Keyword>().Distinct().OrderBy(x => x.FullPath))
        outKeywords.Add(keyword);

      return outKeywords;
    }
  }
}
