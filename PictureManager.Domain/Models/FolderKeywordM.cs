using System.Collections.Generic;
using System.IO;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordM : TreeItem {
    public string FullPath => Tree.GetFullName(this, Path.DirectorySeparatorChar.ToString(), x => x.Name);
    public List<FolderM> Folders { get; } = new();

    public FolderKeywordM(string name, ITreeItem parent) : base(Res.IconFolderPuzzle, name) {
      Parent = parent;
    }

    public override void OnIsExpandedChanged(bool value) {
      if (!value) return;
      foreach (var folder in Folders)
        folder.LoadSubFolders(false);
    }
  }
}
