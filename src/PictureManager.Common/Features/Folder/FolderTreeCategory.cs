using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Folder;

public sealed class FolderTreeCategory : TreeCategory<FolderM> {
  public FolderTreeCategory(FolderR r, TreeView treeView) :
    base(treeView, Res.IconFolder, "Folders", (int)Category.Folders, r) {
    CanMoveItem = true;
    CanCopyItem = true;
    UseTreeDelete = true;
  }

  public override bool CanDrop(object? src, ITreeItem? dest) {
    switch (src) {
      case FolderM srcData: {
        // Folder
        if (dest is FolderM destData
            && !destData.HasThisParent(srcData)
            && !ReferenceEquals(srcData, destData)
            && destData.IsAccessible
            && !ReferenceEquals(srcData.Parent, destData))
          return true;

        break;
      }
      case string[] dragged: {
        // MediaItems
        if (Core.VM.MediaItem.Views.Current == null) break;

        var selected = Core.VM.MediaItem.Views.Current.Selected.Items
          .Select(p => p.FilePath)
          .OrderBy(p => p)
          .ToArray();

        if (selected.SequenceEqual(dragged.OrderBy(x => x))
            && dest is FolderM { IsAccessible: true })
          return true;

        break;
      }
    }

    return false;
  }

  public override async Task OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    if (dest is not FolderM destFolder) return;
    var mode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

    switch (src) {
      case FolderM srcData: // Folder
        if (await Dialog.ShowAsync(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} folder",
              $"Do you really want to {(copy ? "copy" : "move")} folder\n'{srcData.Name}' to '{dest.Name}'?",
              MH.UI.Res.IconQuestion,
              true)) != 1)
          return;

        await CopyMoveU.CopyMoveFolder(srcData, destFolder, mode);

        break;

      case string[]: // MediaItems
        var items = Core.VM.MediaItem.Views.Current?.Selected.Items.OfType<RealMediaItemM>().ToArray();
        if (items == null || items.Length == 0) return;
        if (await Dialog.ShowAsync(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} media items",
              $"Do you really want to {(copy ? "copy" : "move")} {"{0} media item{1}".Plural(items.Length)} to\n'{dest.Name}'?",
              MH.UI.Res.IconQuestion,
              true)) != 1)
          return;

        await CopyMoveU.CopyMoveMediaItems(items, destFolder, mode);

        break;
    }

    Core.VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
  }

  public void AddDrives() {
    foreach (var drive in Drives.SerialNumbers) {
      var di = new DriveInfo(drive.Key);

      // add Drive to the database and to the tree if not already there
      if (Items.Cast<DriveM>().SingleOrDefault(x => x.SerialNumber.Equals(drive.Value, StringComparison.OrdinalIgnoreCase)) is
          not { } item) {
        item = Core.R.Folder.AddDrive(this, drive.Key, drive.Value);
      }

      item.IsAccessible = di.IsReady;
      item.Icon = FolderS.GetDriveIcon(di.DriveType);
      item.Name = drive.Key;

      // add placeholder so the Drive can be expanded
      if (di.IsReady && item.Items.Count == 0)
        item.Items.Add(FolderS.FolderPlaceHolder);
    }
  }

  public void ScrollTo(FolderM? folder) {
    if (folder == null || !Core.S.Viewer.CanViewerSee(folder)) return;
    folder.IsExpanded = true;
    Core.VM.MainWindow.TreeViewCategories.Select(TreeView);
    TreeView.ScrollTo(folder);
  }
}