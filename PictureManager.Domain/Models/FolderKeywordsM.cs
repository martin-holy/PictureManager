using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordsM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly Core _core;
    private int _maxId;

    public List<FolderKeywordM> All { get; } = new();
    public event EventHandler ReloadEvent = delegate { };
    public static readonly FolderKeywordM FolderKeywordPlaceHolder = new(0, string.Empty, null);

    public FolderKeywordsM(Core core) {
      _core = core;
    }

    public void Load() {
      _maxId = 0;

      foreach (var fk in All) {
        fk.Folders.Clear();
        fk.Items.Clear();
      }

      Items.Clear();
      All.Clear();

      foreach (var folder in _core.FoldersM.All.Where(x => x.IsFolderKeyword))
        LoadRecursive(folder, this);

      ReloadEvent(this, EventArgs.Empty);
    }

    private void LoadRecursive(ITreeBranch folder, ITreeBranch fkRoot) {
      foreach (var f in folder.Items.OfType<FolderM>()) {
        var fk = GetForFolder(f, fkRoot);
        LinkWithFolder(f, fk);
        LoadRecursive(f, fk);
      }
    }

    private FolderKeywordM GetForFolder(FolderM folder, ITreeBranch fkRoot) {
      var fk = fkRoot.Items.Cast<FolderKeywordM>()
        .SingleOrDefault(x => x.Name.Equals(folder.Name, StringComparison.Ordinal));

      if (fk == null) {
        // remove placeholder
        if (Items.Count == 1 && FolderKeywordPlaceHolder.Equals(Items[0])) Items.Clear();

        fk = new(GetNextId(), folder.Name, fkRoot);
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

    private int GetNextId() => ++_maxId;
  }
}
