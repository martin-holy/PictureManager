using MH.UI.BaseClasses;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class FoldersTreeCategory : TreeCategory<FolderM> {
  public FoldersTreeCategory(FoldersDA da) :
    base(Res.IconFolder, "Folders", (int)Category.Folders) {
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;

    CanMoveItem = true;
    CanCopyItem = true;
    UseTreeDelete = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<FolderM> e) =>
    TreeView.ScrollTo(e.Data, false);

  public override void OnItemSelected(object o) {
    // SHIFT key => recursive
    // MBL => show, MBL+ctrl => and, MBL+alt => hide
    if (o is not ITreeItem item) return;
    if (Core.MediaViewerM.IsVisible)
      Core.VM.MainWindow.IsInViewMode = false;

    _ = Core.MediaItemsViews.LoadByFolder(item);
  }

  public override bool CanDrop(object src, ITreeItem dest) {
    switch (src) {
      case FolderM srcData: {
        // Folder
        if (dest is FolderM destData
            && !destData.HasThisParent(srcData)
            && !Equals(srcData, destData)
            && destData.IsAccessible
            && !Equals(srcData.Parent, destData))
          return true;

        break;
      }
      case string[] dragged: {
        // MediaItems
        if (Core.MediaItemsViews.Current == null) break;

        var selected = Core.MediaItemsViews.Current.Selected.Items
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

  public override void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    if (dest is not FolderM destFolder) return;
    var foMode = copy
      ? FileOperationMode.Copy
      : FileOperationMode.Move;

    switch (src) {
      case FolderM srcData: // Folder
        if (Dialog.Show(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} folder",
              $"Do you really want to {(copy ? "copy" : "move")} folder '{srcData.Name}' to '{dest.Name}'?",
              Res.IconQuestion,
              true)) != 1)
          return;

        Core.FoldersM.CopyMove(foMode, srcData, destFolder);

        break;

      case string[]: // MediaItems
        var items = Core.MediaItemsViews.Current.Selected.Items.OfType<RealMediaItemM>().ToList();
        // TODO mi rewrite (don't do anything if count is 0)
        if (Dialog.Show(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} media items",
              $"Do you really want to {(copy ? "copy" : "move")} {"{0} media item{1}".Plural(items.Count)} to '{dest.Name}'?",
              Res.IconQuestion,
              true)) != 1)
          return;

        Core.MediaItemsM.CopyMove(foMode, items, destFolder);

        break;
    }

    Core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
  }

  public void AddDrives() {
    foreach (var drive in Drives.SerialNumbers) {
      var di = new DriveInfo(drive.Key);

      // add Drive to the database and to the tree if not already there
      if (Items.Cast<DriveM>().SingleOrDefault(x => x.SerialNumber.Equals(drive.Value, StringComparison.OrdinalIgnoreCase)) is
          not { } item) {
        item = Core.Db.Folders.AddDrive(this, drive.Key, drive.Value);
        Items.Add(item);
      }

      item.IsAccessible = di.IsReady;
      item.Icon = FoldersM.GetDriveIcon(di.DriveType);
      item.Name = drive.Key;

      // add placeholder so the Drive can be expanded
      if (di.IsReady && item.Items.Count == 0)
        item.Items.Add(FoldersM.FolderPlaceHolder);
    }
  }

  public void ScrollTo(FolderM folder) {
    if (folder == null || !Core.ViewersM.CanViewerSee(folder)) return;
    folder.IsExpanded = true;
    Core.TreeViewCategoriesM.Select(TreeView);
    TreeView.ScrollTo(folder);
  }
}