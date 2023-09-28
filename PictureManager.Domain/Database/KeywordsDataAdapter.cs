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

  public KeywordsM Model { get; }

  public KeywordsDataAdapter(Db db) : base("Keywords", 3) {
    _db = db;
    _db.ReadyEvent += OnDbReady;
    Model = new(this);
  }

  private void OnDbReady(object sender, EventArgs args) {
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
    SaveToFile(GetAll<KeywordM>(Model.TreeCategory));

  public override KeywordM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], null);

  public override string ToCsv(KeywordM keyword) =>
    string.Join("|",
      keyword.GetHashCode().ToString(),
      keyword.Name,
      (keyword.Parent as KeywordM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    _db.CategoryGroups.LinkGroups(Model.TreeCategory, AllDict);

    // link hierarchical keywords
    foreach (var (keyword, csv) in AllCsv) {
      // reference to parent and back reference to children
      if (!string.IsNullOrEmpty(csv[2])) {
        keyword.Parent = AllDict[int.Parse(csv[2])];
        keyword.Parent.Items.Add(keyword);
      }
    }

    // add loose keywords
    foreach (var keywordM in AllDict.Values.Where(x => x.Parent == null)) {
      keywordM.Parent = Model.TreeCategory;
      Model.TreeCategory.Items.Add(keywordM);
    }

    // group for keywords automatically added from MediaItems metadata
    Model.TreeCategory.AutoAddedGroup = Model.TreeCategory.Items
      .OfType<CategoryGroupM>()
      .SingleOrDefault(x => x.Name.Equals("Auto Added"));
  }

  public override KeywordM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name, parent));

  public override void ItemDelete(ITreeItem item) =>
    TreeItemDelete((KeywordM)item);

  public override string ValidateNewItemName(ITreeItem parent, string name) =>
    parent.Items.OfType<KeywordM>().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
      ? $"{name} item already exists!"
      : null;
}