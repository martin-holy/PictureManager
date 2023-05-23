using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class KeywordsM : TreeCategoryBase {
    private readonly CategoryGroupsM _categoryGroupsM;

    public KeywordsDataAdapter DataAdapter { get; set; }
    public CategoryGroupM AutoAddedGroup { get; set; }

    public event EventHandler<ObjectEventArgs<KeywordM>> KeywordDeletedEventHandler = delegate { };

    public KeywordsM(CategoryGroupsM categoryGroupsM) : base(Res.IconTagLabel, Category.Keywords, "Keywords") {
      _categoryGroupsM = categoryGroupsM;
      CanMoveItem = true;
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new KeywordM(DataAdapter.GetNextId(), name, root);
      Tree.SetInOrder(root.Items, item, x => x.Name);
      DataAdapter.All.Add(item.Id, item);

      return item;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      item.Name = name;
      Tree.SetInOrder(item.Parent.Items, item, x => x.Name);
      DataAdapter.IsModified = true;
    }

    protected override void ModelItemDelete(ITreeItem item) {
      var keywords = new List<KeywordM>();
      Tree.GetThisAndItemsRecursive(item, ref keywords);
      item.Parent.Items.Remove(item);

      foreach (var keyword in keywords) {
        keyword.Parent = null;
        keyword.Items = null;
        DataAdapter.All.Remove(keyword.Id);
        KeywordDeletedEventHandler(this, new(keyword));
        DataAdapter.IsModified = true;
      }
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      root.Items.OfType<KeywordM>().Any(x => x.Name.Equals(name, StringComparison.CurrentCulture))
        ? $"{name} item already exists!"
        : null;

    protected override void ModelGroupCreate(ITreeItem root, string name) =>
      _categoryGroupsM.GroupCreate(name, Category);

    protected override void ModelGroupRename(ITreeGroup group, string name) =>
      _categoryGroupsM.GroupRename(group, name);

    protected override void ModelGroupDelete(ITreeGroup group) =>
      _categoryGroupsM.GroupDelete(group);

    public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
      _categoryGroupsM.GroupMove(group, dest, aboveDest);

    protected override string ValidateNewGroupName(ITreeItem root, string name) =>
      CategoryGroupsM.ItemCanRename(root, name)
        ? null
        : $"{name} group already exists!";

    // TODO refactor using Items and not All
    public KeywordM GetByFullPath(string fullPath) {
      if (string.IsNullOrEmpty(fullPath)) return null;

      var pathNames = fullPath.Split('/');

      // get top level Keyword => Parent is not Keyword but Keywords or CategoryGroup
      var keyword = DataAdapter.All.Values.SingleOrDefault(x => x.Parent is not KeywordM && x.Name.Equals(pathNames[0]));

      // return Keyword if it was found and is 1 level type
      if (keyword != null && pathNames.Length == 1)
        return keyword;

      // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
      var root = keyword?.Parent ?? AutoAddedGroup;

      // for each keyword in pathNames => find or create
      foreach (var name in pathNames)
        root = root.Items.OfType<KeywordM>().SingleOrDefault(x => x.Name.Equals(name))
          ?? ModelItemCreate(root, name);

      return root as KeywordM;
    }

    public static List<KeywordM> Toggle(List<KeywordM> list, KeywordM keyword) {
      list ??= new();

      var allKeywords = new List<KeywordM>();
      foreach (var k in list)
        Tree.GetThisAndParentRecursive(k, ref allKeywords);

      if (allKeywords.Any(x => x.Id.Equals(keyword.Id))) {
        list.Remove(keyword);
        if (list.Count == 0)
          list = null;
      }
      else {
        // remove possible redundant keywords 
        // example: if new keyword is "Weather/Sunny" keyword "Weather" is redundant
        var newKeywords = new List<KeywordM>();
        Tree.GetThisAndParentRecursive(keyword, ref newKeywords);
        foreach (var newKeyword in newKeywords)
          list.Remove(newKeyword);

        list.Add(keyword);
      }

      return list;
    }

    public static IEnumerable<KeywordM> GetAllKeywords(IEnumerable<KeywordM> keywords) {
      if (keywords == null)
        return Enumerable.Empty<KeywordM>();

      var allKeywords = new List<KeywordM>();

      foreach (var keyword in keywords)
        Tree.GetThisAndParentRecursive(keyword, ref allKeywords);

      return allKeywords.Distinct().OrderBy(x => x.FullName).ToArray();
    }
  }
}
