using MH.UI.Controls;
using MH.Utils.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Folder;

public sealed class FolderTreeView : TreeView {
  public FolderTreeCategory Category { get; }

  public FolderTreeView(FolderR r) {
    Category = new(r, this);
  }

  protected override Task _onItemSelected(ITreeItem item, CancellationToken token) =>
    Core.VM.MediaItem.Views.LoadByFolder(item);
}