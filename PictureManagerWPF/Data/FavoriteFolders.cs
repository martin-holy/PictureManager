﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Properties;

namespace PictureManager.Data {
  public class FavoriteFolders : BaseItem {
    public ObservableCollection<FavoriteFolder> Items { get; set; }

    public FavoriteFolders() {
      Items = new ObservableCollection<FavoriteFolder>();
    }

    public void Load() {
      Items.Clear();
      foreach (string path in Settings.Default.FolderFavorites.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x)) {
        var p = path.EndsWith("\\") ? path.Substring(0, path.Length - 1) : path;
        int lio = p.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
        string label = p.Substring(lio + 1, p.Length - lio - 1);
        Items.Add(new FavoriteFolder {Title = label, FullPath = p, IconName = "appbar_folder"});
      }
    }

    public void Remove(string path) {
      var lines = Settings.Default.FolderFavorites.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
      lines.Remove(path);
      Settings.Default.FolderFavorites = string.Join(Environment.NewLine, lines);
      Settings.Default.Save();
    }

    public void Add(string path) {
      bool found = Settings.Default.FolderFavorites.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0;
      if (!found) {
        if (!Settings.Default.FolderFavorites.EndsWith(Environment.NewLine)) {
          Settings.Default.FolderFavorites += Environment.NewLine;
        }
        Settings.Default.FolderFavorites += path;
        Settings.Default.Save();
      }
    }
  }
}
