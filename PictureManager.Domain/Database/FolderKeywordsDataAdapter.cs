using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

// TODO why is TreeDataAdapter param of type FolderM?
/// <summary>
/// DB fields: ID
/// </summary>
public class FolderKeywordsDataAdapter : TreeDataAdapter<FolderM> {
  private readonly Db _db;

  public FolderKeywordsTreeCategory Model { get; }

  public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);
  public List<FolderKeywordM> All2 { get; } = new();

  public FolderKeywordsDataAdapter(Db db) : base("FolderKeywords", 1) {
    _db = db;
    _db.ReadyEvent += OnDbReady;
    Model = new(this);
  }

  private void OnDbReady(object sender, EventArgs args) {
    _db.Folders.ItemCreatedEvent += (_, e) =>
      LoadIfContains((FolderM)e.Data.Parent);

    _db.Folders.ItemRenamedEvent += (_, e) =>
      LoadIfContains(e.Data);

    _db.Folders.ItemsDeletedEvent += (_, _) =>
      Reload();
  }

  public override void Save() =>
    SaveDriveRelated(All
      .GroupBy(x => Tree.GetParentOf<DriveM>(x).Name)
      .ToDictionary(x => x.Key, x => x.Select(y => y)));

  public override FolderM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), string.Empty, null);

  public override string ToCsv(FolderM folder) =>
    folder.GetHashCode().ToString();

  public override void LinkReferences() {
    foreach (var id in AllDict.Keys)
      AllDict[id] = _db.Folders.AllDict[id];
  }

  public void LoadIfContains(FolderM folder) {
    if (All.Contains(folder) || folder.FolderKeyword != null)
      Reload();
  }

  public void Reload() {
    foreach (var fk in All2) {
      fk.Folders.Clear();
      fk.Items.Clear();
    }

    Model.Items.Clear();
    All2.Clear();

    if (AllDict == null)
      foreach (var folder in All)
        LoadRecursive(folder, Model);
    else
      foreach (var folder in AllDict.Values)
        LoadRecursive(folder, Model);

    foreach (var fk in All2) {
      if (fk.Folders.All(x => !_db.Folders.Model.IsFolderVisible(x)))
        fk.IsHidden = true;
    }
  }

  private void LoadRecursive(ITreeItem folder, ITreeItem fkRoot) {
    foreach (var f in folder.Items.OfType<FolderM>()) {
      var fk = GetForFolder(f, fkRoot);
      LinkWithFolder(f, fk);
      LoadRecursive(f, fk);
    }
  }

  private FolderKeywordM GetForFolder(FolderM folder, ITreeItem fkRoot) {
    var fk = fkRoot.Items.Cast<FolderKeywordM>()
      .SingleOrDefault(x => x.Name.Equals(folder.Name, StringComparison.OrdinalIgnoreCase));

    if (fk == null) {
      // remove placeholder
      if (Model.Items.Count == 1 && FolderKeywordPlaceHolder.Equals(Model.Items[0]))
        Model.Items.Clear();

      fk = new(folder.Name, fkRoot);
      fkRoot.Items.SetInOrder(fk, x => ((FolderKeywordM)x).Name);
      All2.Add(fk);
    }

    return fk;
  }

  private static void LinkWithFolder(FolderM f, FolderKeywordM fk) {
    f.FolderKeyword = fk;
    fk.Folders.Add(f);
  }

  public void LinkFolderWithFolderKeyword(FolderM folder, FolderKeywordM folderKeyword) =>
    LinkWithFolder(folder, GetForFolder(folder, folderKeyword));

  public void SetAsFolderKeyword(FolderM folder) {
    All.Add(folder);
    IsModified = true;
    Reload();
  }

  protected override void OnItemDeleted(FolderM item) =>
    Reload();
}