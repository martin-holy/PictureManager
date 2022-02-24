﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MH.UI.WPF.BaseClasses;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class StatusPanelVM : ObservableObject {
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };
    private bool _isPinned = true;
    private MediaItemM _currentMediaItemM;

    public bool IsPinned { get => _isPinned; set { _isPinned = value; OnPropertyChanged(); } }

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
        var fileSize = App.Core.ThumbnailsGridsM.Current?.ActiveFileSize;
        if (fileSize != null)
          return fileSize;

        if (CurrentMediaItemM == null)
          return string.Empty;

        try {
          var size = new FileInfo(CurrentMediaItemM.FilePath).Length;

          return size == 0
            ? string.Empty
            : IOExtensions.FileSizeToString(size);
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

        if (!App.Ui.MediaViewerVM.IsVisible || CurrentMediaItemM.Folder.FolderKeyword == null) {
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

    public RelayCommand<object> PinCommand { get; }

    public StatusPanelVM(Core core) {
      core.MediaItemsM.PropertyChanged += (_, e) => {
        if (nameof(core.MediaItemsM.Current).Equals(e.PropertyName)) {
          CurrentMediaItemM = null;
          CurrentMediaItemM = core.MediaItemsM.Current;
        }
      };

      PinCommand = new(() => IsPinned = !IsPinned);
    }

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < CurrentMediaItemM?.Rating; i++)
        Rating.Add(0);
    }
  }
}
