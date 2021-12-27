using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public MediaItemM CurrentMediaItemM {
      get => _currentMediaItemM;
      set {
        _currentMediaItemM = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DateAndTime));
        OnPropertyChanged(nameof(FilePath));
        UpdateRating();
      }
    }

    public ObservableCollection<string> FilePath {
      get {
        var paths = new ObservableCollection<string>();
        if (CurrentMediaItemM == null) return paths;

        if (App.Ui.AppInfo.AppMode == AppMode.Browser || CurrentMediaItemM.Folder.FolderKeyword == null) {
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

    // TODO this prop is here because binding to App.WMain... doesn't work
    public double? ActualZoom => App.WMain?.MediaViewer.FullImage.ActualZoom;
    public string DateAndTime => DateTimeExtensions.DateTimeFromString(CurrentMediaItemM?.FileName, _dateFormats, "H:mm:ss");
    public ObservableCollection<IconName> Rating { get; } = new();

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
        Rating.Add(IconName.Star);
    }
  }
}
