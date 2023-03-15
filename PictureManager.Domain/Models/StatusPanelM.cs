﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;

namespace PictureManager.Domain.Models {
  public sealed class StatusPanelM : ObservableObject {
    private readonly Core _core;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };
    private MediaItemM _currentMediaItemM;

    // TODO remove this
    public MediaItemM CurrentMediaItemM {
      get => _currentMediaItemM;
      set {
        _currentMediaItemM = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DateAndTime));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(FileSize));
        UpdateRating();
      }
    }

    public string FileSize {
      get {
        try {
          var items = _core.MediaItemsM.GetActive();

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
        if (CurrentMediaItemM == null) return paths;

        if (CurrentMediaItemM.Folder.FolderKeyword == null) {
          paths.Add(CurrentMediaItemM.FilePath);
          return paths;
        }

        var fks = new List<FolderKeywordM>();
        MH.Utils.Tree.GetThisAndParentRecursive(CurrentMediaItemM.Folder.FolderKeyword, ref fks);
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Name.FirstIndexOfLetter();

            if (fk.Name.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
          }

        var fileName = string.IsNullOrEmpty(DateAndTime) ? CurrentMediaItemM.FileName : CurrentMediaItemM.FileName[15..];
        paths.Add(fileName);

        return paths;
      }
    }

    public string DateAndTime => DateTimeExtensions.DateTimeFromString(CurrentMediaItemM?.FileName, _dateFormats, "H:mm:ss");
    public ObservableCollection<int> Rating { get; } = new();

    public StatusPanelM(Core core) {
      _core = core;

      core.MediaItemsM.PropertyChanged += (_, e) => {
        if (nameof(core.MediaItemsM.Current).Equals(e.PropertyName)) {
          CurrentMediaItemM = null;
          CurrentMediaItemM = core.MediaItemsM.Current;
        }
      };
    }

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < CurrentMediaItemM?.Rating; i++)
        Rating.Add(0);
    }
  }
}
