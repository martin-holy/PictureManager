﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models;

public class FolderM : TreeItem, IEquatable<FolderM> {
  #region IEquatable implementation
  public bool Equals(FolderM other) => Id == other?.Id;
  public override bool Equals(object obj) => Equals(obj as FolderM);
  public override int GetHashCode() => Id;
  public static bool operator ==(FolderM a, FolderM b) => a?.Equals(b) ?? b is null;
  public static bool operator !=(FolderM a, FolderM b) => !(a == b);
  #endregion

  public int Id { get; }
  public List<RealMediaItemM> MediaItems { get; } = new();
  public FolderKeywordM FolderKeyword { get; set; }
  public bool IsAccessible { get; set; }
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);
  public string FullPathCache => FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.CachePath);

  public FolderM(int id, string name, ITreeItem parent) : base(Res.IconFolder, name) {
    Id = id;
    Parent = parent;
  }

  protected override void OnIsExpandedChanged(bool value) {
    if (value) LoadSubFolders(false);
  }

  public void LoadSubFolders(bool recursive) {
    // remove placeholder
    if (Items.Count == 1 && ReferenceEquals(FoldersM.FolderPlaceHolder, Items[0])) Items.Clear();

    if (!Directory.Exists(FullPath)) return;

    var dirNames = new HashSet<string>();
    var fullPath = FullPath + Path.DirectorySeparatorChar;

    foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
      var dirName = dir[fullPath.Length..];
      var folder = this.GetByName(dirName) ?? Core.Db.Folders.ItemCreate(this, dirName);
      folder.IsHidden = !Core.ViewersM.CanViewerSee(folder);
      dirNames.Add(dirName);
    }

    // remove Folders deleted outside of this application
    foreach (var item in Items.Where(x => !dirNames.Contains(x.Name)).Cast<FolderM>().ToArray())
      Core.Db.Folders.TreeItemDelete(item);

    // add placeholder so the folder can be expanded or keep loading if recursive
    foreach (var item in Items.Cast<FolderM>()) {
      // folder already have some items
      if (!recursive && item.Items.Count > 0) {
        item.IsAccessible = true;
        continue;
      }

      try {
        if (recursive) {
          item.LoadSubFolders(true);
        }
        else {
          if (Directory.EnumerateDirectories(item.FullPath).Any()) {
            item.Items.Add(FoldersM.FolderPlaceHolder);
            item.FolderKeyword?.Items.Add(FolderKeywordsDA.FolderKeywordPlaceHolder);
          }
          item.IsAccessible = true;
        }
      }
      catch (UnauthorizedAccessException) {
        item.IsAccessible = false;
        item.Icon = Res.IconFolderLock;
      }
    }

    Items.Sort(x => ((FolderM)x).Name);
  }
}

public static class FolderExtensions {
  public static FolderM GetByName(this FolderM item, string name) =>
    item.Items.GetByName(name, StringComparison.OrdinalIgnoreCase) as FolderM;

  public static IEnumerable<RealMediaItemM> GetMediaItems(this FolderM folder, bool recursive) =>
    recursive
      ? folder.Flatten().SelectMany(x => x.MediaItems)
      : folder.MediaItems;
}