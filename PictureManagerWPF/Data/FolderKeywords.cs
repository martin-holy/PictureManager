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
        var path = (string) row[1];

        path = GetFolderKeywordPath(path);
        if (string.IsNullOrEmpty(path)) continue;

        paths.Add(id, path);
      }

      foreach (var keyPath in paths.Select(p => p.Value).Distinct().OrderBy(p => p)) {
        FolderKeyword newItem = new FolderKeyword {
          IconName = "appbar_folder",
          FullPath = keyPath, 
          FolderIds = string.Join(",", paths.Where(p => p.Value.Equals(keyPath)).Select(p => p.Key))
        };

        if (!newItem.FullPath.Contains("\\")) {
          newItem.Title = newItem.FullPath;
          Items.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('\\') + 1);
          FolderKeyword parentFolderKeyword = GetFolderKeywordByFullPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('\\')), true);
          if (parentFolderKeyword == null) continue;
          newItem.Parent = parentFolderKeyword;
          parentFolderKeyword.Items.Add(newItem);
        }
      }  
    }

    public string GetFolderKeywordPath(string path) {
      path = Settings.Default.FolderKeywordIngnoreList.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
        .OrderBy(x => x)
        .Aggregate(path, (current, ignorePath) => current.Replace(ignorePath, string.Empty));
      return path.Contains(":") ? string.Empty : path;
    }

    public FolderKeyword GetFolderKeywordByFullPath(string fullPath, bool create) {
      FolderKeyword parent = null;
      ObservableCollection<FolderKeyword> root = Items;

      while (true) {
        if (root.Count == 0 || string.IsNullOrEmpty(fullPath)) return null;

        string[] keyParts = fullPath.Split('\\');
        FolderKeyword folderKeyword = root.FirstOrDefault(fk => fk.Title.Equals(keyParts[0]));
        if (folderKeyword == null) {
          if (!create) return null;
          folderKeyword = CreateFolderKeyword(root, parent, keyParts[0]);
        }
        if (keyParts.Length <= 1) return folderKeyword;

        parent = folderKeyword;
        root = folderKeyword.Items;
        fullPath = fullPath.Substring(keyParts[0].Length + 1);
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

      FolderKeyword folderKeyword =
        root.FirstOrDefault(fk => string.Compare(fk.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      root.Insert(folderKeyword == null ? 0 : root.IndexOf(folderKeyword), newFolderKeyword);
      return newFolderKeyword;
    }
  }
}
