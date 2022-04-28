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

      ExpandedChangedEventHandler += (o, _) => {
        if (o is not FolderKeywordM fk || !fk.IsExpanded) return;

        foreach (var folder in fk.Folders)
          folder.LoadSubFolders(false);
      };
    }
  }
}
