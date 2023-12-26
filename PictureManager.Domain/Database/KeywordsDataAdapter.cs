using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Parent
/// </summary>
public class KeywordsDataAdapter : TreeDataAdapter<KeywordM> {
  private readonly Db _db;
  private const string _notFoundRecordNamePrefix = "Not found ";

  public KeywordsM Model { get; }

  public KeywordsDataAdapter(Db db) : base("Keywords", 3) {
    _db = db;
    _db.ReadyEvent += delegate { OnDbReady(); };
    Model = new(this);
  }

  private void OnDbReady() {
    // move all group items to root
    _db.CategoryGroups.ItemDeletedEvent += (_, e) => {
      if (e.Data.Category != Category.Keywords) return;
      foreach (var item in e.Data.Items.ToArray())
        ItemMove(item, Model.TreeCategory, false);
    };
  }

  public static IEnumerable<T> GetAll<T>(ITreeItem root) {
    if (root is T rootItem)
      yield return rootItem;

    foreach (var item in root.Items)
      foreach (var subItem in GetAll<T>(item))
        yield return subItem;
  }

  public override void Save() =>
    SaveToSingleFile(GetAll<KeywordM>(Model.TreeCategory));

  public override KeywordM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], null);

  public override string ToCsv(KeywordM keyword) =>
    string.Join("|",
      keyword.GetHashCode().ToString(),
      keyword.Name,
      (keyword.Parent as KeywordM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    _db.CategoryGroups.LinkGroups(Model.TreeCategory, AllDict);
    LinkTree(Model.TreeCategory, 2);

    // group for keywords automatically added from MediaItems metadata
    Model.TreeCategory.AutoAddedGroup = Model.TreeCategory.Items
      .OfType<CategoryGroupM>()
      .SingleOrDefault(x => x.Name.Equals("Auto Added"));
  }

  public List<KeywordM> Link(string csv, IDataAdapter seeker) =>
    LinkList(csv, GetNotFoundRecord, seeker);

  private KeywordM GetNotFoundRecord(int notFoundId) {
    var id = GetNextId();
    var item = new KeywordM(id, $"{_notFoundRecordNamePrefix}{id} ({notFoundId})", Model.TreeCategory);
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
      items.FirstOrDefault(x => !x.HasThisParent(Model.TreeCategory.AutoAddedGroup))
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
          ? Model.TreeCategory.AutoAddedGroup
          : GetFirst(last), path) },
        1 => new[] { found[0] },
        _ => found
      };

      first = false;
    }

    return GetFirst(last) as KeywordM;
  }
}