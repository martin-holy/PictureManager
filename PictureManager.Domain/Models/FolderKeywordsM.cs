using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordsM : TreeCategoryBase {
    public FolderKeywordsDataAdapter DataAdapter { get; set; }
    public List<FolderKeywordM> All { get; } = new();
    public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(string.Empty, null);
    public RelayCommand<FolderM> SetAsFolderKeywordCommand { get; }

    public FolderKeywordsM() : base(Res.IconFolderPuzzle, Category.FolderKeywords, "Folder Keywords") {
      SetAsFolderKeywordCommand = new(SetAsFolderKeyword);
    }

    public override void OnItemSelect(MouseButtonEventArgs e) =>
      Core.Instance.FoldersM.OnItemSelect(e);

    public void LoadIfContains(FolderM folder) {
      if (DataAdapter.All.Contains(folder) || folder.FolderKeyword != null)
        Load();
    }

    public void Load() {
      foreach (var fk in All) {
        fk.Folders.Clear();
        fk.Items.Clear();
      }

      Items.Clear();
      All.Clear();

      if (DataAdapter.AllDict == null)
        foreach (var folder in DataAdapter.All)
          LoadRecursive(folder, this);
      else
        foreach (var folder in DataAdapter.AllDict.Values)
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

    private void SetAsFolderKeyword(FolderM folder) {
      DataAdapter.All.Add(folder);
      DataAdapter.IsModified = true;
      Load();
    }

    public void Remove(FolderM folder) {
      DataAdapter.All.Remove(folder);
      DataAdapter.IsModified = true;
      Load();
    }
  }
}
