﻿using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Common.TreeCategories;

public sealed class FoldersTreeCategory : TreeCategory<FolderM> {
  public FoldersTreeCategory(FolderR r) :
    base(Res.IconFolder, "Folders", (int)Category.Folders) {
    DataAdapter = r;
    CanMoveItem = true;
    CanCopyItem = true;
    UseTreeDelete = true;
  }

  public override void OnItemSelected(object o) {
    // SHIFT key => recursive
    // MBL => show, MBL+ctrl => and, MBL+alt => hide
    if (o is not FolderM && o is not FolderKeywordM) return;
    if (Core.VM.MediaViewer.IsVisible)
      Core.VM.MainWindow.IsInViewMode = false;

    _ = Core.VM.MediaItem.Views.LoadByFolder((ITreeItem)o);
  }

  public override bool CanDrop(object src, ITreeItem dest) {
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

  public override void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    if (dest is not FolderM destFolder) return;
    var mode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

    switch (src) {
      case FolderM srcData: // Folder
        if (Dialog.Show(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} folder",
              $"Do you really want to {(copy ? "copy" : "move")} folder '{srcData.Name}' to '{dest.Name}'?",
              MH.UI.Res.IconQuestion,
              true)) != 1)
          return;

        CopyMoveU.CopyMoveFolder(srcData, destFolder, mode);

        break;

      case string[]: // MediaItems
        var items = Core.VM.MediaItem.Views.Current.Selected.Items.OfType<RealMediaItemM>().ToArray();
        // TODO mi rewrite (don't do anything if count is 0)
        if (Dialog.Show(new MessageDialog(
              $"{(copy ? "Copy" : "Move")} media items",
              $"Do you really want to {(copy ? "copy" : "move")} {"{0} media item{1}".Plural(items.Length)} to '{dest.Name}'?",
              MH.UI.Res.IconQuestion,
              true)) != 1)
          return;

        CopyMoveU.CopyMoveMediaItems(items, destFolder, mode);

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
        Items.Add(item);
      }

      item.IsAccessible = di.IsReady;
      item.Icon = FolderS.GetDriveIcon(di.DriveType);
      item.Name = drive.Key;

      // add placeholder so the Drive can be expanded
      if (di.IsReady && item.Items.Count == 0)
        item.Items.Add(FolderS.FolderPlaceHolder);
    }
  }

  public void ScrollTo(FolderM folder) {
    if (folder == null || !Core.S.Viewer.CanViewerSee(folder)) return;
    folder.IsExpanded = true;
    Core.VM.MainWindow.TreeViewCategories.Select(TreeView);
    TreeView.ScrollTo(folder);
  }
}