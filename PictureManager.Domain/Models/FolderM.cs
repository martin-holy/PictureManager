﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models {
    public class FolderM : TreeItem, IEquatable<FolderM> {
    #region IEquatable implementation
    public bool Equals(FolderM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as FolderM);
    public override int GetHashCode() => Id;
    public static bool operator ==(FolderM a, FolderM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(FolderM a, FolderM b) => !(a == b);
    #endregion

    private bool _isAccessible;

    public int Id { get; }
    public List<RealMediaItemM> MediaItems { get; } = new();
    public FolderKeywordM FolderKeyword { get; set; }
    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); UpdateIcon(); } }
    public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);
    public string FullPathCache => FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.CachePath);

    public FolderM(int id, string name, ITreeItem parent) : base(Res.IconFolder, name) {
      Id = id;
      Parent = parent;
    }

    public override void OnIsExpandedChanged(bool value) {
      if (value) LoadSubFolders(false);
      UpdateIcon();
    }

    public void UpdateIcon() {
      if (Parent is FoldersTreeCategory) return;

      Icon = IsExpanded
        ? Res.IconFolderOpen
        : !IsAccessible
          ? Res.IconFolderLock
          : Res.IconFolder;
    }

    public async void LoadSubFolders(bool recursive) {
      // remove placeholder
      if (Items.Count == 1 && FoldersM.FolderPlaceHolder.Equals(Items[0])) Items.Clear();

      var dirNames = new HashSet<string>();
      var fullPath = FullPath + Path.DirectorySeparatorChar;
      
      // using task to wake up drive async
      var dirExists = new Task<bool>(() => Directory.Exists(fullPath));
      dirExists.Start();
      if (!await dirExists) return;

      foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
        var dirName = dir[fullPath.Length..];
        dirNames.Add(dirName);

        // get existing Folder in the tree
        var folder = Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(dirName, StringComparison.OrdinalIgnoreCase));
        
        if (folder != null) {
          folder.IsHidden = !Core.ViewersM.CanViewerSee(folder);
          continue;
        }

        // add new Folder to the database
        Core.Db.Folders.ItemCreate(this, dirName);
      }

      // remove Folders deleted outside of this application
      foreach (var item in Items.Cast<FolderM>().ToArray()) {
        if (dirNames.Contains(item.Name)) continue;
        Core.Db.Folders.TreeItemDelete(item);
      }

      // add placeholder so the folder can be expanded
      // or if recursive => keep loading
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
        }
      }

      // sort Items
      Items.Sort(x => ((FolderM)x).Name);
    }

    public RealMediaItemM GetMediaItemByName(string fileName) =>
      MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName, StringComparison.Ordinal));

    public List<RealMediaItemM> GetMediaItems(bool recursive) {
      if (!recursive) return MediaItems;

      // get all Folders
      var folders = new List<FolderM>();
      Tree.GetThisAndItemsRecursive(this, ref folders);

      // get all MediaItems from folders
      var mis = new List<RealMediaItemM>();
      foreach (var f in folders)
        mis.AddRange(f.MediaItems);

      return mis;
    }
  }
}
