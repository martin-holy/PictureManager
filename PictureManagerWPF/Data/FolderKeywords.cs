using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using PictureManager.Properties;

namespace PictureManager.Data {
  public class FolderKeywords: BaseItem {
    public ObservableCollection<FolderKeyword> Items { get; set; }
    public DbStuff Db;

    public FolderKeywords() {
      Items = new ObservableCollection<FolderKeyword>();
    }

    public void Load() {
      Items.Clear();
      Dictionary<int, string> paths = new Dictionary<int, string>();
      const string sql = "select Id, Path from Directories order by Path";

      foreach (DataRow row in Db.Select(sql)) {
        var id = (int) (long) row[0];
        var fullPath = (string) row[1];

        var path = GetFolderKeywordKeyPath(fullPath);
        if (string.IsNullOrEmpty(path)) continue;

        paths.Add(id, path);
      }

      foreach (var keyPath in paths.Select(p => p.Value).Distinct().OrderBy(p => p)) {
        FolderKeyword newItem = new FolderKeyword {
          IconName = "appbar_folder",
          FullPath = keyPath, 
          FolderIdList = paths.Where(p => p.Value.Equals(keyPath)).Select(p => p.Key).ToList()
        };

        if (!newItem.FullPath.Contains("\\")) {
          newItem.Title = newItem.FullPath;
          Items.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('\\') + 1);
          FolderKeyword parentFolderKeyword = GetFolderKeywordByKeyPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('\\')), true);
          if (parentFolderKeyword == null) continue;
          newItem.Parent = parentFolderKeyword;
          parentFolderKeyword.Items.Add(newItem);
        }
      }  
    }

    public string GetFolderKeywordKeyPath(string fullPath) {
      var keyPath = Settings.Default.FolderKeywordIngnoreList.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
        .OrderBy(x => x)
        .Aggregate(fullPath, (current, ignorePath) => current.Replace(ignorePath, string.Empty));
      return keyPath.Contains(":") ? string.Empty : keyPath;
    }

    public FolderKeyword GetFolderKeywordByDirId(int dirId) {
      string fullPath = (string) Db.ExecuteScalar($"select Path from Directories where Id = {dirId}");
      return GetFolderKeywordByKeyPath(GetFolderKeywordKeyPath(fullPath), false);
    }

    public FolderKeyword GetFolderKeywordByFullPath(string fullPath) {
      return GetFolderKeywordByKeyPath(GetFolderKeywordKeyPath(fullPath), false);
    }

    public FolderKeyword GetFolderKeywordByKeyPath(string keyPath, bool create) {
      FolderKeyword parent = null;
      ObservableCollection<FolderKeyword> root = Items;

      while (true) {
        if (root.Count == 0 || string.IsNullOrEmpty(keyPath)) return null;

        string[] keyParts = keyPath.Split('\\');
        FolderKeyword folderKeyword = root.FirstOrDefault(fk => fk.Title.Equals(keyParts[0]));
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

    public FolderKeyword CreateFolderKeyword(ObservableCollection<FolderKeyword> root, FolderKeyword parent, string name) {
      string kFullPath = parent == null ? name : $"{parent.FullPath}/{name}";
      FolderKeyword newFolderKeyword = new FolderKeyword {
        IconName = "appbar_folder",
        FullPath = kFullPath,
        Title = name,
        Parent = parent
      };

      root.Add(newFolderKeyword);
      return newFolderKeyword;
    }
  }
}
