﻿using MH.UI.BaseClasses;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PictureManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Name|Category|GroupItems
/// </summary>
public class CategoryGroupR(CoreR coreR) : TreeDataAdapter<CategoryGroupM>(coreR, "CategoryGroups", 4) {
  private readonly List<ITreeItem> _categories = [];

  public override void Save() =>
    SaveToSingleFile(_categories.SelectMany(x => x.Items.OfType<CategoryGroupM>()));

  public override CategoryGroupM FromCsv(string[] csv) {
    var category = (Category)int.Parse(csv[2]);
    return GetNew(int.Parse(csv[0]), csv[1], category);
  }

  public override string ToCsv(CategoryGroupM cg) =>
    string.Join("|",
      cg.GetHashCode().ToString(),
      cg.Name,
      (int)cg.Category,
      cg.Items.ToHashCodes().ToCsv());

  public void LinkGroups<TI>(TreeCategory cat, Dictionary<int, TI> allDict) where TI : class, ITreeItem {
    cat.Items.Clear();

    foreach (var (cg, csv) in AllCsv) {
      if (!cat.Id.Equals((int)cg.Category)) continue;

      var items = string.IsNullOrEmpty(csv[3]) ? [] : csv[3].Split(',').Select(int.Parse);
      
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
    var cat = (Category)Tree.GetParentOf<ITreeCategory>(parent)!.Id;
    var group = GetNew(GetNextId(), name, cat);
    group.Parent = parent;
    group.Items.CollectionChanged += GroupItems_CollectionChanged;

    return TreeItemCreate(group);
  }

  private static CategoryGroupM GetNew(int id, string name, Category cat) =>
    cat switch {
      Category.Keywords => new KeywordCategoryGroupM(id, name, cat, Res.CategoryToIcon(cat)),
      Category.People => new PersonCategoryGroupM(id, name, cat, Res.CategoryToIcon(cat)),
      _ => throw new NotSupportedException()
    };

  public override string? ValidateNewItemName(ITreeItem parent, string? name) {
    if (string.IsNullOrEmpty(name)) return "The name is empty!";
    return parent.Items
      .OfType<CategoryGroupM>()
      .Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ? $"{name} group already exists!"
        : null;
  }

  protected override void OnItemDeleted(object sender, CategoryGroupM item) {
    item.Parent?.Items.Remove(item);
  }

  private void GroupItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
    if (coreR.IsReady) IsModified = true;
  }

  public void AddCategory(ITreeItem cat) {
    _categories.Add(cat);
  }
}