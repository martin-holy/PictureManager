using MH.UI.BaseClasses;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class FoldersTreeCategory : TreeCategory<FolderM> {
  public FoldersTreeCategory() : base(Res.IconFolder, "Folders", (int)Category.Folders) {
    DataAdapter = Core.Db.Folders = new(this);
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    DataAdapter.ItemRenamedEvent += OnItemRenamed;

    CanMoveItem = true;
    CanCopyItem = true;
  }

  private void OnItemCreated(object sender, ObjectEventArgs<FolderM> e) =>
    TreeView.ScrollTo(e.Data);

  private void OnItemRenamed(object sender, ObjectEventArgs<FolderM> e) {
    // reload if the folder was selected before
    if (e.Data.IsSelected)
      OnItemSelected(e.Data);
  }

  public override void OnItemSelected(object o) {
    // SHIFT key => recursive
    // MBL => show, MBL+ctrl => and, MBL+alt => hide
    if (o is not ITreeItem item) return;
    if (Core.MediaViewerM.IsVisible)
      Core.MainWindowM.IsFullScreen = false;

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

        var selected = Core.MediaItemsViews.Current.FilteredItems
          .Where(x => x.IsSelected)
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

        // reload last selected source if was moved
        if (foMode == FileOperationMode.Move && srcData.IsSelected &&
            Tree.GetByPath(destFolder, srcData.Name, Path.DirectorySeparatorChar) != null) {
          destFolder.ExpandTo();
          OnItemSelected(destFolder);
        }

        break;

      case string[]: // MediaItems
        if (Dialog.Show(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} media items",
              $"Do you really want to {(copy ? "copy" : "move")} media items to '{dest.Name}'?",
              Res.IconQuestion,
              true)) != 1)
          return;

        Core.MediaItemsM.CopyMove(foMode,
          Core.MediaItemsViews.Current.FilteredItems.Where(x => x.IsSelected).ToList(),
          destFolder);
        Core.MediaItemsM.DataAdapter.IsModified = true;

        break;
    }

    Core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
  }

  public void AddDrives() {
    var drives = Environment.GetLogicalDrives();
    var drivesNames = new List<string>();

    foreach (var drive in drives) {
      var di = new DriveInfo(drive);
      var driveName = di.Name.TrimEnd(Path.DirectorySeparatorChar);
      drivesNames.Add(driveName);

      // add Drive to the database and to the tree if not already exists
      if (Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase)) is
          not { } item) {
        item = Core.Db.Folders.AddDrive(this, driveName);
        Items.Add(item);
      }

      item.IsAccessible = di.IsReady;
      item.Icon = FoldersM.GetDriveIcon(di.DriveType);

      // add placeholder so the Drive can be expanded
      if (di.IsReady && item.Items.Count == 0)
        item.Items.Add(FoldersM.FolderPlaceHolder);
    }

    // set available drives
    foreach (var item in Items.Cast<FolderM>()) {
      item.IsAvailable = drivesNames.Any(x => x.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
      item.IsHidden = !Core.FoldersM.IsFolderVisible(item);
    }
  }

  public void ScrollTo(FolderM folder) {
    if (folder == null || !Core.FoldersM.IsFolderVisible(folder)) return;
    folder.IsExpanded = true;
    Core.TreeViewCategoriesM.Select(TreeView);
    TreeView.ScrollTo(folder);
  }
}