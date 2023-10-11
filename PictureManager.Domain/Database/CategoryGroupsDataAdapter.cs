using MH.UI.BaseClasses;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Category|GroupItems
/// </summary>
public class CategoryGroupsDataAdapter : TreeDataAdapter<CategoryGroupM> {
  private readonly List<ITreeCategory> _categories = new();

  public CategoryGroupsDataAdapter() : base("CategoryGroups", 4) { }

  public override void Save() =>
    SaveToFile(_categories.SelectMany(x => x.Items.OfType<CategoryGroupM>()));

  public override CategoryGroupM FromCsv(string[] csv) {
    var category = (Category)int.Parse(csv[2]);
    return new(int.Parse(csv[0]), csv[1], category, Res.CategoryToIcon(category));
  }

  public override string ToCsv(CategoryGroupM cg) =>
    string.Join("|",
      cg.GetHashCode().ToString(),
      cg.Name,
      (int)cg.Category,
      string.Join(",", cg.Items
        .Select(x => x.GetHashCode().ToString())));

  public void LinkGroups<TI>(TreeCategory cat, Dictionary<int, TI> allDict) where TI : class, ITreeItem {
    cat.Items.Clear();

    foreach (var (cg, csv) in AllCsv) {
      if (!cat.Id.Equals((int)cg.Category)) continue;

      var items = string.IsNullOrEmpty(csv[3])
        ? Enumerable.Empty<int>()
        : csv[3].Split(',').Select(int.Parse);
      
      cg.Parent = cat;
      cg.Parent.Items.Add(cg);

      foreach (var item in items.Where(allDict.ContainsKey).Select(id => allDict[id])) {
        item.Parent = cg;
        cg.Items.Add(item);
      }

      cg.Items.CollectionChanged += GroupItems_CollectionChanged;
    }
  }

  public override CategoryGroupM ItemCreate(ITreeItem parent, string name) {
    var cat = (Category)Tree.GetParentOf<ITreeCategory>(parent).Id;
    var group = new CategoryGroupM(GetNextId(), name, cat, Res.CategoryToIcon(cat)) { Parent = parent };
    group.Items.CollectionChanged += GroupItems_CollectionChanged;

    return TreeItemCreate(group);
  }

  public override string ValidateNewItemName(ITreeItem parent, string name) =>
    parent.Items
      .OfType<CategoryGroupM>()
      .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ? $"{name} group already exists!"
        : null;

  protected override void OnItemDeleted(CategoryGroupM item) {
    item.Parent.Items.Remove(item);
  }

  private void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
    IsModified = true;
  }

  public void AddCategory(ITreeCategory<CategoryGroupM> cat) {
    _categories.Add(cat);
    cat.SetGroupDataAdapter(this);
  }
}