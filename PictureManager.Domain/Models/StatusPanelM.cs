﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class StatusPanelM : ObservableObject {
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    public string FileSize {
      get {
        try {
          var items = Core.MediaItemsM.GetActive();

          return items.Any()
            ? IOExtensions.FileSizeToString(
              items.Sum(mi => new FileInfo(mi.FilePath).Length))
            : string.Empty;
        }
        catch {
          return string.Empty;
        }
      }
    }

    public ObservableCollection<string> FilePath {
      get {
        var paths = new ObservableCollection<string>();
        if (Core.MediaItemsM.Current == null) return paths;

        if (Core.MediaItemsM.Current.Folder.FolderKeyword == null) {
          paths.Add(Core.MediaItemsM.Current.FilePath);
          return paths;
        }

        var fks = Core.MediaItemsM.Current.Folder.FolderKeyword.GetThisAndParents().ToList();
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Name.FirstIndexOfLetter();

            if (fk.Name.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
          }

        var fileName = string.IsNullOrEmpty(DateAndTime)
          ? Core.MediaItemsM.Current.FileName
          : Core.MediaItemsM.Current.FileName[15..];
        paths.Add(fileName);

        return paths;
      }
    }

    public string DateAndTime =>
      DateTimeExtensions.DateTimeFromString(Core.MediaItemsM.Current?.FileName, _dateFormats, "H:mm:ss");
    
    public ObservableCollection<int> Rating { get; } = new();

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < Core.MediaItemsM.Current?.Rating; i++)
        Rating.Add(0);
    }

    public void Update() {
      OnPropertyChanged(nameof(DateAndTime));
      OnPropertyChanged(nameof(FilePath));
      OnPropertyChanged(nameof(FileSize));
      UpdateRating();
    }
  }
}
