using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Parent
/// </summary>
public class KeywordsDA : TreeDataAdapter<KeywordM> {
  private readonly Db _db;
  private const string _notFoundRecordNamePrefix = "Not found ";

  public KeywordsTreeCategory Tree { get; }

  public KeywordsDA(Db db) : base("Keywords", 3) {
    _db = db;
    Tree = new(this);
  }

  public static IEnumerable<T> GetAll<T>(ITreeItem root) {
    if (root is T rootItem)
      yield return rootItem;

    foreach (var item in root.Items)
      foreach (var subItem in GetAll<T>(item))
        yield return subItem;
  }

  public override void Save() =>
    SaveToSingleFile(GetAll<KeywordM>(Tree));

  public override KeywordM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], null);

  public override string ToCsv(KeywordM keyword) =>
    string.Join("|",
      keyword.GetHashCode().ToString(),
      keyword.Name,
      (keyword.Parent as KeywordM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    _db.CategoryGroups.LinkGroups(Tree, AllDict);
    LinkTree(Tree, 2);

    // group for keywords automatically added from MediaItems metadata
    Tree.AutoAddedGroup = Tree.Items
      .OfType<CategoryGroupM>()
      .SingleOrDefault(x => x.Name.Equals("Auto Added"));
  }

  public List<KeywordM> Link(string csv, IDataAdapter seeker) =>
    LinkList(csv, GetNotFoundRecord, seeker);

  private KeywordM GetNotFoundRecord(int notFoundId) {
    var id = GetNextId();
    var item = new KeywordM(id, $"{_notFoundRecordNamePrefix}{id} ({notFoundId})", Tree);
    item.Parent.Items.Add(item);
    IsModified = true;
    return item;
  }

  public override KeywordM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name, parent));

  public override string ValidateNewItemName(ITreeItem parent, string name) =>
    parent.Items.OfType<KeywordM>().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
      ? $"{name} item already exists!"
      : null;

  public KeywordM GetByFullPath(string fullPath) {
    if (string.IsNullOrEmpty(fullPath)) return null;

    ITreeItem GetFirst(ITreeItem[] items) =>
      items.FirstOrDefault(x => !x.HasThisParent(Tree.AutoAddedGroup))
      ?? items.FirstOrDefault();

    var first = true;
    ITreeItem[] last = Array.Empty<ITreeItem>();

    foreach (var path in fullPath.Split('/')) {
      var found = (first
          ? All.Where(x => x.Parent is not KeywordM)
          : last.SelectMany(x => x.Items))
        .Where(x => x.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
        .ToArray();

      last = found.Length switch {
        0 => new[] { (ITreeItem)ItemCreate(first
          ? Tree.AutoAddedGroup
          : GetFirst(last), path) },
        1 => new[] { found[0] },
        _ => found
      };

      first = false;
    }

    return GetFirst(last) as KeywordM;
  }

  public void MoveGroupItemsToRoot(CategoryGroupM group) {
    if (group.Category != Category.Keywords) return;
    foreach (var item in group.Items.ToArray())
      ItemMove(item, Tree, false);
  }
}