﻿using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;

namespace PictureManager.Common.Features.FolderKeyword;

public sealed class FolderKeywordTreeCategory : TreeCategory<FolderM> {
  public static RelayCommand<FolderM> SetAsFolderKeywordCommand { get; set; } = null!;

  public FolderKeywordTreeCategory(FolderKeywordR r, TreeView treeView) :
    base(treeView, Res.IconFolderPuzzle, "Folder Keywords", (int)Category.FolderKeywords, r) {
    SetAsFolderKeywordCommand = new(x => r.SetAsFolderKeyword(x!), x => x != null, null, "Set as Folder Keyword");
  }
}