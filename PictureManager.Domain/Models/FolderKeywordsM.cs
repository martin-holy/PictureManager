using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordsM : TreeCategoryBase {
    public List<FolderKeywordM> All { get; } = new();
    public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);

    public FolderKeywordsM() : base(Res.IconFolderPuzzle, Category.FolderKeywords, "Folder Keywords") { }

    public void Load(IEnumerable<FolderM> folders) {
      foreach (var fk in All) {
        fk.Folders.Clear();
        fk.Items.Clear();
      }

      Items.Clear();
      All.Clear();

      foreach (var folder in folders.Where(x => x.IsFolderKeyword))
        LoadRecursive(folder, this);

      foreach (var fk in All) {
        if (fk.Folders.All(x => !Core.Instance.FoldersM.IsFolderVisible(x)))
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
        if (Items.Count == 1 && FolderKeywordPlaceHolder.Equals(Items[0])) Items.Clear();

        fk = new(folder.Name, fkRoot);
        fkRoot.Items.SetInOrder(fk, x => ((FolderKeywordM)x).Name);
        All.Add(fk);
      }

      return fk;
    }

    private static void LinkWithFolder(FolderM f, FolderKeywordM fk) {
      f.FolderKeyword = fk;
      fk.Folders.Add(f);
    }

    public void LinkFolderWithFolderKeyword(FolderM folder, FolderKeywordM folderKeyword) =>
      LinkWithFolder(folder, GetForFolder(folder, folderKeyword));
  }
}
