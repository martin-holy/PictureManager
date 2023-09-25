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
  private readonly FolderKeywordsTreeCategory _model;

  public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);
  public List<FolderKeywordM> All2 { get; } = new();

  public FolderKeywordsDataAdapter(FolderKeywordsTreeCategory model) : base("FolderKeywords", 1) {
    _model = model;
    Core.Db.ReadyEvent += OnDbReady;
  }

  private void OnDbReady(object sender, EventArgs args) {
    Core.Db.Folders.ItemCreatedEvent += (_, e) =>
      LoadIfContains((FolderM)e.Data.Parent);

    Core.Db.Folders.ItemRenamedEvent += (_, e) =>
      LoadIfContains(e.Data);

    Core.Db.Folders.ItemsDeletedEvent += (_, _) =>
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
      AllDict[id] = Core.Db.Folders.AllDict[id];
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

    _model.Items.Clear();
    All2.Clear();

    if (AllDict == null)
      foreach (var folder in All)
        LoadRecursive(folder, _model);
    else
      foreach (var folder in AllDict.Values)
        LoadRecursive(folder, _model);

    foreach (var fk in All2) {
      if (fk.Folders.All(x => !Core.FoldersM.IsFolderVisible(x)))
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
      if (_model.Items.Count == 1 && FolderKeywordPlaceHolder.Equals(_model.Items[0]))
        _model.Items.Clear();

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