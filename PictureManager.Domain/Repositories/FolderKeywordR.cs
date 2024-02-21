using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Repositories;

// TODO why is TreeDataAdapter param of type FolderM?
/// <summary>
/// DB fields: ID
/// </summary>
public class FolderKeywordR : TreeDataAdapter<FolderM> {
  private readonly CoreR _coreR;

  public FolderKeywordsTreeCategory Tree { get; }

  public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);
  public List<FolderKeywordM> All2 { get; } = [];

  public FolderKeywordR(CoreR coreR) : base("FolderKeywords", 1) {
    _coreR = coreR;
    IsDriveRelated = true;
    Tree = new(this);
  }

  public override Dictionary<string, IEnumerable<FolderM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(All, x => x);

  public override FolderM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), string.Empty, null);

  public override string ToCsv(FolderM folder) =>
    folder.GetHashCode().ToString();

  public override void LinkReferences() {
    foreach (var id in AllDict.Keys)
      AllDict[id] = _coreR.Folder.AllDict[id];
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

    Tree.Items.Clear();
    All2.Clear();

    foreach (var folder in All)
      LoadRecursive(folder, Tree);

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
      if (Tree.Items.Count == 1 && ReferenceEquals(FolderKeywordPlaceHolder, Tree.Items[0]))
        Tree.Items.Clear();

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