﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Parent|IsFolderKeyword
  /// </summary>
  public sealed class FolderM : ObservableObject, IEquatable<FolderM>, IRecord, ITreeBranch {
    #region IEquatable implementation
    public bool Equals(FolderM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as FolderM);
    public override int GetHashCode() => Id;
    public static bool operator ==(FolderM a, FolderM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(FolderM a, FolderM b) => !(a == b);
    #endregion

    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public List<MediaItemM> MediaItems { get; } = new();
    public FolderKeywordM FolderKeyword { get; set; }

    private string _name;
    private bool _isAccessible;
    private bool _isAvailable;
    private bool _isFolderKeyword;
    private string _iconName;

    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); } }
    public bool IsAvailable { get => _isAvailable; set { _isAvailable = value; OnPropertyChanged(); } }
    public bool IsFolderKeyword { get => _isFolderKeyword; set { _isFolderKeyword = value; OnPropertyChanged(); } }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string FullPath => Tree.GetFullName(this, Path.DirectorySeparatorChar.ToString(), x => x.Name);
    public string FullPathCache => FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath);

    public FolderM(int id, string name, ITreeBranch parent) {
      Id = id;
      Name = name;
      Parent = parent;
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
        if (folder != null) continue;

        // add new Folder to the database
        folder = new(Core.Instance.FoldersM.DataAdapter.GetNextId(), dirName, this);
        Core.Instance.FoldersM.All.Add(folder);

        // add new Folder to the tree
        Items.Add(folder);

        if (FolderKeyword != null)
          Core.Instance.FolderKeywordsM.LinkFolderWithFolderKeyword(this, FolderKeyword);
      }

      // remove Folders deleted outside of this application
      foreach (var item in Items.Cast<FolderM>().ToArray()) {
        if (dirNames.Contains(item.Name)) continue;
        Core.Instance.FoldersM.ItemDelete(item);
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
            if (Directory.EnumerateDirectories(item.FullPath).GetEnumerator().MoveNext()) {
              item.Items.Add(FoldersM.FolderPlaceHolder);
              item.FolderKeyword?.Items.Add(FolderKeywordsM.FolderKeywordPlaceHolder);
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

    public bool HasThisParent(FolderM parent) {
      var p = Parent as FolderM;
      while (p != null) {
        if (p.Id.Equals(parent.Id))
          return true;
        p = p.Parent as FolderM;
      }

      return false;
    }

    /// <param name="path">full or partial folder path with no directory separator on the end</param>
    public FolderM GetByPath(string path) {
      if (string.IsNullOrEmpty(path)) return null;
      if (FullPath.Equals(path, StringComparison.Ordinal)) return this;

      var root = this;
      var pathParts = (path.StartsWith(FullPath, StringComparison.CurrentCultureIgnoreCase)
        ? path[(FullPath.Length + 1)..]
        : path)
        .Split(Path.DirectorySeparatorChar);

      foreach (var pathPart in pathParts) {
        var folder = root.Items.Cast<FolderM>().SingleOrDefault(x => x.Name.Equals(pathPart, StringComparison.OrdinalIgnoreCase));
        if (folder == null) return null;
        root = folder;
      }

      return root;
    }

    public MediaItemM GetMediaItemByName(string fileName) =>
      MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName, StringComparison.Ordinal));

    public List<MediaItemM> GetMediaItems(bool recursive) {
      if (!recursive) return MediaItems;

      // get all Folders
      var folders = new List<FolderM>();
      Tree.GetThisAndItemsRecursive(this, ref folders);

      // get all MediaItems from folders
      var mis = new List<MediaItemM>();
      foreach (var f in folders)
        mis.AddRange(f.MediaItems);

      return mis;
    }
  }
}
