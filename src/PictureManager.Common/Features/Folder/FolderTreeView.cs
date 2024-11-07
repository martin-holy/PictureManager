﻿using MH.UI.Controls;
using MH.Utils.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Folder;

public sealed class FolderTreeView : TreeView<ITreeItem> {
  public FolderTreeCategory Category { get; }

  public FolderTreeView(FolderR r) {
    Category = new(r, this);
  }

  protected override Task OnItemSelected(ITreeItem item, CancellationToken token) =>
    Core.VM.MediaItem.Views.LoadByFolder(item);
}