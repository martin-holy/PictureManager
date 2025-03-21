﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Folder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.FolderKeyword;

// TODO why is TreeDataAdapter param of type FolderM?
/// <summary>
/// DB fields: ID
/// </summary>
public class FolderKeywordR : TreeDataAdapter<FolderM> {
  private readonly CoreR _coreR;

  public FolderKeywordTreeView Tree { get; }

  public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);
  public List<FolderKeywordM> All2 { get; } = [];

  public FolderKeywordR(CoreR coreR) : base(coreR, "FolderKeywords", 1) {
    _coreR = coreR;
    IsDriveRelated = true;
    Tree = new(this);
  }

  protected override Dictionary<string, IEnumerable<FolderM>> _getAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x);

  protected override FolderM _fromCsv(string[] csv) =>
    new(int.Parse(csv[0]), string.Empty, null);

  protected override string _toCsv(FolderM folder) =>
    folder.GetHashCode().ToString();

  public override void LinkReferences() {
    foreach (var id in AllDict.Keys)
      AllDict[id] = _coreR.Folder.AllDict[id];
  }

  public void LoadIfContains(FolderM? folder) {
    if (folder != null && (All.Contains(folder) || folder.FolderKeyword != null))
      Reload();
  }

  public void Reload() {
    foreach (var fk in All2) {
      fk.Folders.Clear();
      fk.Items.Clear();
    }

    Tree.Category.Items.Clear();
    All2.Clear();

    foreach (var folder in All)
      LoadRecursive(folder, Tree.Category);

    foreach (var fk in All2.Where(fk => fk.Folders.All(x => !Core.S.Viewer.CanViewerSee(x))))
      fk.IsHidden = true;
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
      if (Tree.Category.Items.Count == 1 && ReferenceEquals(FolderKeywordPlaceHolder, Tree.Category.Items[0]))
        Tree.Category.Items.Clear();

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

  protected override void _onItemDeleted(object sender, FolderM item) =>
    Reload();
}