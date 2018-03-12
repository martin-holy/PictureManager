using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using PictureManager.Properties;

namespace PictureManager.ViewModel {
  public sealed class FolderKeywords: BaseCategoryItem {
    public List<FolderKeyword> AllFolderKeywords;

    public FolderKeywords() : base (Category.FolderKeywords) {
      AllFolderKeywords = new List<FolderKeyword>();
      Title = "Folder Keywords";
      IconName = "appbar_folder";
    }

    public void Load() {
      Items.Clear();
      AllFolderKeywords.Clear();
      var paths = new Dictionary<int, string>();
      foreach (var dir in ACore.Db.Directories.OrderBy(x => x.Path)) {
        if (!Directory.Exists(dir.Path)) continue;
        if (!ACore.CanViewerSeeThisDirectory(dir.Path)) continue;
        var path = GetFolderKeywordKeyPath(dir.Path);
        if (string.IsNullOrEmpty(path)) continue;

        paths.Add(dir.Id, path);
      }

      foreach (var keyPath in paths.Select(p => p.Value).Distinct().OrderBy(p => p)) {
        var newItem = new FolderKeyword {
          FullPath = keyPath, 
          FolderIdList = paths.Where(p => p.Value.Equals(keyPath)).Select(p => p.Key).ToList()
        };

        if (!newItem.FullPath.Contains("\\")) {
          newItem.Title = newItem.FullPath;
          Items.Add(newItem);
          AllFolderKeywords.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('\\') + 1);
          var parentFolderKeyword = GetFolderKeywordByKeyPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('\\')), true);
          if (parentFolderKeyword == null) continue;
          newItem.Parent = parentFolderKeyword;
          parentFolderKeyword.Items.Add(newItem);
          AllFolderKeywords.Add(newItem);
        }
      }  
    }

    public string GetFolderKeywordKeyPath(string fullPath) {
      foreach (var ignorePath in Settings.Default.FolderKeywordIngnoreList.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
        if (fullPath.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase)) {
          return fullPath.Substring(ignorePath.Length);
        }
      }
      return string.Empty;
    }

    public FolderKeyword GetFolderKeywordByDirId(ObservableCollection<BaseTreeViewItem> items, int dirId) {
      foreach (var folderKeyword in items.Cast<FolderKeyword>()) {
        if (folderKeyword.FolderIdList.Any(fid => fid == dirId)) return folderKeyword;
        var fk = GetFolderKeywordByDirId(folderKeyword.Items, dirId);
        if (fk != null) return fk;
      }
      return null;
    }

    public FolderKeyword GetFolderKeywordByFullPath(string fullPath) {
      return GetFolderKeywordByKeyPath(GetFolderKeywordKeyPath(fullPath));
    }

    public FolderKeyword GetFolderKeywordByKeyPath(string keyPath) {
      return AllFolderKeywords.SingleOrDefault(x => x.FullPath == keyPath);
    }

    public FolderKeyword GetFolderKeywordByKeyPath(string keyPath, bool create) {
      FolderKeyword parent = null;
      var root = Items;

      while (true) {
        if (root.Count == 0 || string.IsNullOrEmpty(keyPath)) return null;

        var keyParts = keyPath.Split('\\');
        var folderKeyword = root.Cast<FolderKeyword>().FirstOrDefault(fk => fk.Title.Equals(keyParts[0]));
        if (folderKeyword == null) {
          if (!create) return null;
          folderKeyword = CreateFolderKeyword(root, parent, keyParts[0]);
        }
        if (keyParts.Length <= 1) return folderKeyword;

        parent = folderKeyword;
        root = folderKeyword.Items;
        keyPath = keyPath.Substring(keyParts[0].Length + 1);
      }
    }

    public FolderKeyword CreateFolderKeyword(ObservableCollection<BaseTreeViewItem> root, FolderKeyword parent, string name) {
      var kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      var newFolderKeyword = new FolderKeyword {
        FullPath = kFullPath,
        Title = name,
        Parent = parent
      };

      root.Add(newFolderKeyword);
      return newFolderKeyword;
    }
  }
}
