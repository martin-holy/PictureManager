using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Folder;
using System.Collections.Generic;
using System.IO;

namespace PictureManager.Common.Features.FolderKeyword;

public sealed class FolderKeywordM : TreeItem {
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);
  public List<FolderM> Folders { get; } = [];

  public FolderKeywordM(string name, ITreeItem? parent) : base(Res.IconFolderPuzzle, name) {
    Parent = parent;
  }

  protected override void _onIsExpandedChanged(bool value) {
    if (!value) return;
    foreach (var folder in Folders)
      folder.LoadSubFolders(false);
  }
}