﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Services;
using PictureManager.Common.TreeCategories;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Name|Parent
/// </summary>
public class FolderR : TreeDataAdapter<FolderM>, IFolderR {
  public FoldersTreeCategory Tree { get; }

  public FolderR(CoreR coreR) : base(coreR, "Folders", 3) {
    IsDriveRelated = true;
    Tree = new(this);
  }

  public static IEnumerable<T> GetAll<T>(ITreeItem root) {
    yield return (T)root;

    foreach (var item in root.Items)
    foreach (var subItem in GetAll<T>(item))
      if (!ReferenceEquals(FolderS.FolderPlaceHolder, subItem))
        yield return subItem;
  }

  public override Dictionary<string, IEnumerable<FolderM>> GetAsDriveRelated() =>
    Tree.Items.ToDictionary(x => x.Name, GetAll<FolderM>);

  public override FolderM FromCsv(string[] csv) =>
    string.IsNullOrEmpty(csv[2])
      ? new DriveM(int.Parse(csv[0]), csv[1], null, CurrentVolumeSerialNumber)
      : new FolderM(int.Parse(csv[0]), csv[1], null);

  public override string ToCsv(FolderM folder) =>
    string.Join("|",
      folder.GetHashCode().ToString(),
      folder.Name,
      (folder.Parent as FolderM)?.GetHashCode().ToString() ?? string.Empty);

  public override void LinkReferences() {
    Tree.Items.Clear();
    LinkTree(Tree, 2);
  }

  public override FolderM ItemCreate(ITreeItem parent, string name) {
    Directory.CreateDirectory(IOExtensions.PathCombine(((FolderM)parent).FullPath, name));

    return TreeItemCreate(new(GetNextId(), name, parent) { IsAccessible = true });
  }

  public override void ItemRename(ITreeItem item, string name) {
    var folder = (FolderM)item;
    var parent = (FolderM)item.Parent;

    Directory.Move(folder.FullPath, IOExtensions.PathCombine(parent.FullPath, name));
    if (Directory.Exists(folder.FullPathCache))
      Directory.Move(folder.FullPathCache, IOExtensions.PathCombine(parent.FullPathCache, name));

    base.ItemRename(item, name);
  }

  public override string ValidateNewItemName(ITreeItem parent, string name) {
    // check if folder already exists
    if (Directory.Exists(IOExtensions.PathCombine(((FolderM)parent).FullPath, name)))
      return "Folder already exists!";

    // check if is correct folder name
    if (Path.GetInvalidPathChars().Any(name.Contains))
      return "New folder's name contains incorrect character(s)!";

    return null;
  }

  protected override void OnItemDeleted(object sender, FolderM item) {
    if (!Core.R.IsCopyMoveInProgress) DeleteFromDrive(item);
  }

  public DriveM AddDrive(ITreeItem parent, string name, string sn) {
    var item = new DriveM(GetNextId(), name, parent, sn);
    IsModified = true;
    All.Add(item);
    return item;
  }

  public static void DeleteFromDrive(FolderM item) {
    var path = item.FullPath;
    var cachePath = item.FullPathCache;
    if (Directory.Exists(path)) CoreR.FileOperationDelete([path], true, false);
    if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true);
  }

  IFolderM IFolderR.GetFolder(string folderPath) => GetFolder(folderPath);

  public FolderM GetFolder(string folderPath) {
    if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return null;
    var parts = Path.GetFullPath(folderPath).Split(Path.DirectorySeparatorChar);
    if (Tree.Items.GetByName(parts[0], StringComparison.OrdinalIgnoreCase) is not { } folder) return null;

    for (int i = 1; i < parts.Length; i++) {
      ((FolderM)folder).RemovePlaceHolder();
      folder = folder.Items.GetByName(parts[i], StringComparison.OrdinalIgnoreCase)
               ?? ItemCreate(folder, parts[i]);
    }

    return folder as FolderM;
  }
}