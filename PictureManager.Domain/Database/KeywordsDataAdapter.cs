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
public class KeywordsDataAdapter : TreeDataAdapter<KeywordM> {
  private readonly KeywordsTreeCategory _model;

  public KeywordsDataAdapter(KeywordsTreeCategory model) : base("Keywords", 3) {
    _model = model;
    Core.Db.ReadyEvent += OnDbReady;
  }

  private void OnDbReady(object sender, EventArgs args) {
    // move all group items to root
    Core.Db.CategoryGroups.ItemDeletedEvent += (_, e) => {
      if (e.Data.Category != Category.Keywords) return;
      foreach (var item in e.Data.Items.ToArray())
        ItemMove(item, _model, false);
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
    SaveToFile(GetAll<KeywordM>(_model));

  public override KeywordM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], null);

  public override string ToCsv(KeywordM keyword) =>
    string.Join("|",
      keyword.GetHashCode().ToString(),
      keyword.Name,
      (keyword.Parent as KeywordM)?.GetHashCode().ToString());

  public override void LinkReferences() {
    Core.Db.CategoryGroups.LinkGroups(_model, AllDict);

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
      keywordM.Parent = _model;
      _model.Items.Add(keywordM);
    }

    // group for keywords automatically added from MediaItems metadata
    _model.AutoAddedGroup = _model.Items
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