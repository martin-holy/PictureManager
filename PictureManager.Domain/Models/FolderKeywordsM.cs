using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordsM : ITreeBranch {
    #region ITreeBranch implementation
    public object Parent { get; set; }
    public ObservableCollection<object> Items { get; set; } = new();
    #endregion

    private readonly Core _core;
    private int _maxId;

    public List<FolderKeywordM> All { get; } = new();
    public event EventHandler ReloadEvent = delegate { };

    public FolderKeywordsM(Core core) {
      _core = core;
    }

    public void Load() {
      ReloadEvent(this, EventArgs.Empty);
      _maxId = 0;

      foreach (var fk in All) {
        fk.Folders.Clear();
        fk.Items.Clear();
      }

      Items.Clear();
      All.Clear();

      foreach (var folder in _core.Folders.All.Cast<Folder>().Where(x => x.IsFolderKeyword && !x.IsThisOrParentHidden()))
        LoadRecursive(folder, this);
    }

    private void LoadRecursive(ICatTreeViewItem folder, ITreeBranch fkRoot) {
      foreach (var f in folder.Items.OfType<Folder>()) {
        if (!_core.CanViewerSeeThisFolder(f)) continue;
        if (f.IsThisOrParentHidden()) continue;

        var fk = GetForFolder(f, fkRoot);
        LinkWithFolder(f, fk);
        LoadRecursive(f, fk);
      }
    }

    public FolderKeywordM GetForFolder(Folder folder, ITreeBranch fkRoot) {
      var fk = fkRoot.Items.Cast<FolderKeywordM>()
        .SingleOrDefault(x => x.Name.Equals(folder.Title, StringComparison.Ordinal));

      if (fk == null) {
        fk = new(GetNextId(), folder.Title, fkRoot);
        fkRoot.Items.SetInOrder(fk, x => ((FolderKeywordM)x).Name);
        All.Add(fk);
      }

      return fk;
    }

    public static void LinkWithFolder(Folder f, FolderKeywordM fk) {
      f.FolderKeyword = fk;
      fk.Folders.Add(f);
    }

    private int GetNextId() => ++_maxId;
  }
}
