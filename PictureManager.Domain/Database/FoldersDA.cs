﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Parent
/// </summary>
public class FoldersDA : TreeDataAdapter<FolderM> {
  public FoldersM Model { get; }

  public FoldersDA(Db db) : base("Folders", 3) {
    IsDriveRelated = true;
    Model = new(this);
    db.ReadyEvent += delegate { OnDbReady(db); };
  }

  private static void OnDbReady(Db db) {
    db.Images.ItemCreatedEvent += (_, e) =>
      e.Data.Folder.MediaItems.Add(e.Data);

    db.Videos.ItemCreatedEvent += (_, e) =>
      e.Data.Folder.MediaItems.Add(e.Data);
  }

  public static IEnumerable<T> GetAll<T>(ITreeItem root) {
    yield return (T)root;

    foreach (var item in root.Items)
    foreach (var subItem in GetAll<T>(item))
      if (!FoldersM.FolderPlaceHolder.Equals(subItem))
        yield return subItem;
  }

  public override Dictionary<string, IEnumerable<FolderM>> GetAsDriveRelated() =>
    Model.TreeCategory.Items.ToDictionary(x => x.Name, GetAll<FolderM>);

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
    Model.TreeCategory.Items.Clear();
    LinkTree(Model.TreeCategory, 2);
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

  public DriveM AddDrive(ITreeItem parent, string name, string sn) {
    var item = new DriveM(GetNextId(), name, parent, sn);
    IsModified = true;
    All.Add(item);
    return item;
  }
}