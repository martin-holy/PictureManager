using MH.UI.Controls;
using MH.Utils.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.FolderKeyword;

public sealed class FolderKeywordTreeView : TreeView {
  public FolderKeywordTreeCategory Category { get; }

  public FolderKeywordTreeView(FolderKeywordR r) {
    Category = new(r, this);
  }

  protected override Task _onItemSelected(ITreeItem item, CancellationToken token) =>
    Core.VM.MediaItem.Views.LoadByFolder(item);
}