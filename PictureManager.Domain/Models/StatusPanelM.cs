using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class StatusPanelM : ObservableObject {
    private readonly Core _core;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

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
        if (_core.MediaItemsM.Current == null) return paths;

        if (_core.MediaItemsM.Current.Folder.FolderKeyword == null) {
          paths.Add(_core.MediaItemsM.Current.FilePath);
          return paths;
        }

        var fks = new List<FolderKeywordM>();
        MH.Utils.Tree.GetThisAndParentRecursive(_core.MediaItemsM.Current.Folder.FolderKeyword, ref fks);
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Name.FirstIndexOfLetter();

            if (fk.Name.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
          }

        var fileName = string.IsNullOrEmpty(DateAndTime)
          ? _core.MediaItemsM.Current.FileName
          : _core.MediaItemsM.Current.FileName[15..];
        paths.Add(fileName);

        return paths;
      }
    }

    public string DateAndTime =>
      DateTimeExtensions.DateTimeFromString(_core.MediaItemsM.Current?.FileName, _dateFormats, "H:mm:ss");
    
    public ObservableCollection<int> Rating { get; } = new();

    public StatusPanelM(Core core) {
      _core = core;
    }

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < _core.MediaItemsM.Current?.Rating; i++)
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
