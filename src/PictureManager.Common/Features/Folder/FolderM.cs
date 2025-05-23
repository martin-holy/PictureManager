﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.FolderKeyword;
using PictureManager.Common.Features.MediaItem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Common.Features.Folder;

public class FolderM : TreeItem, IEquatable<FolderM> {
  #region IEquatable implementation
  public bool Equals(FolderM? other) => Id == other?.Id;
  public override bool Equals(object? obj) => Equals(obj as FolderM);
  public override int GetHashCode() => Id;
  public static bool operator ==(FolderM? a, FolderM? b) {
    if (ReferenceEquals(a, b)) return true;
    if (a is null || b is null) return false;
    return a.Equals(b);
  }
  public static bool operator !=(FolderM? a, FolderM? b) => !(a == b);
  #endregion

  public int Id { get; }
  public List<RealMediaItemM> MediaItems { get; } = [];
  public FolderKeywordM? FolderKeyword { get; set; }
  public bool IsAccessible { get; set; }
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);
  public string FullPathCache => Core.P.GetFolderPathCache(this);
  public bool HasSubFolders => Items.Count > 0 && !ReferenceEquals(Items[0], FolderS.FolderPlaceHolder);

  public FolderM(int id, string name, ITreeItem? parent) : base(Res.IconFolder, name) {
    Id = id;
    Parent = parent;
  }

  public FolderM? GetByName(string name) =>
    Items.GetByName(name, StringComparison.OrdinalIgnoreCase) as FolderM;

  public IEnumerable<FolderKeywordM> GetFolderKeywords() =>
    FolderKeyword == null ? [] : FolderKeyword.GetThisAndParents();

  public IEnumerable<RealMediaItemM> GetMediaItems(bool recursive) =>
    recursive
      ? this.Flatten().SelectMany(x => x.MediaItems)
      : MediaItems;

  protected override void _onIsExpandedChanged(bool value) {
    if (value) LoadSubFolders(false);
  }

  public void RemovePlaceHolder() {
    if (Items.Count == 1 && ReferenceEquals(FolderS.FolderPlaceHolder, Items[0]))
      Items.Clear();
  }

  public void LoadSubFolders(bool recursive) {
    RemovePlaceHolder();

    if (!Directory.Exists(FullPath)) return;

    var dirNames = new HashSet<string>();
    var fullPath = FullPath + Path.DirectorySeparatorChar;

    foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
      var dirName = dir[fullPath.Length..];
      var folder = GetByName(dirName) ?? Core.R.Folder.ItemCreate(this, dirName);
      folder.IsHidden = !Core.S.Viewer.CanViewerSee(folder);
      dirNames.Add(dirName);
    }

    // remove Folders deleted outside of this application
    foreach (var item in Items.Where(x => !dirNames.Contains(x.Name)).Cast<FolderM>().ToArray())
      Core.R.Folder.TreeItemDelete(item);

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
            item.Items.Add(FolderS.FolderPlaceHolder);
            item.FolderKeyword?.Items.Add(FolderKeywordR.FolderKeywordPlaceHolder);
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