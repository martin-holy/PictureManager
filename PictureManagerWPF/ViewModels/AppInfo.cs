using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PictureManager.ViewModels {
  public class AppInfoRating {
    public IconName IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _progressBarValueA;
    private int _progressBarValueB;
    private bool _progressBarIsIndeterminate;
    private MediaItem _currentMediaItem;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;
    private string _dimension = string.Empty;
    private string _fullGeoName = string.Empty;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    public int ProgressBarValueA { get => _progressBarValueA; set { _progressBarValueA = value; OnPropertyChanged(); } }
    public int ProgressBarValueB { get => _progressBarValueB; set { _progressBarValueB = value; OnPropertyChanged(); } }
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }
    public string Dimension { get => _dimension; set { _dimension = value; OnPropertyChanged(); } }
    public static string ZoomActualFormatted => App.WMain?.FullImage.ZoomActualFormatted;
    public string FullGeoName { get => _fullGeoName; set { _fullGeoName = value; OnPropertyChanged(); } }
    public ObservableCollection<AppInfoRating> Rating { get; } = new();
    public string DateAndTime => Domain.Extensions.DateTimeFromString(CurrentMediaItem?.FileName, _dateFormats, "H:mm:ss");

    public static string FilterAndCount => GetActiveFilterCountFor(BackgroundBrush.AndThis);
    public static string FilterOrCount => GetActiveFilterCountFor(BackgroundBrush.OrThis);
    public static string FilterHiddenCount => GetActiveFilterCountFor(BackgroundBrush.Hidden);

    public AppMode AppMode {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(FilePath));
      }
    }

    public ObservableCollection<string> FilePath {
      get {
        if (CurrentMediaItem == null) return null;
        var paths = new ObservableCollection<string>();

        if (AppMode == AppMode.Browser || CurrentMediaItem.Folder.FolderKeyword == null) {
          paths.Add(CurrentMediaItem.FilePath);
          return paths;
        }

        var fks = new List<ICatTreeViewItem>();
        CatTreeViewUtils.GetThisAndParentRecursive(CurrentMediaItem.Folder.FolderKeyword, ref fks);
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Title.FirstIndexOfLetter();

            if (fk.Title.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Title : fk.Title[startIndex..]);
          }

        var fileName = string.IsNullOrEmpty(DateAndTime) ? CurrentMediaItem.FileName : CurrentMediaItem.FileName[15..];
        paths.Add(fileName);

        return paths;
      }
    }

    public string FileSize {
      get {
        try {
          if (App.Core.MediaItems.ThumbsGrid == null) return string.Empty;

          var size = CurrentMediaItem == null
            ? App.Core.MediaItems.ThumbsGrid.SelectedItems.Sum(mi => new FileInfo(mi.FilePath).Length)
            : new FileInfo(CurrentMediaItem.FilePath).Length;

          return size == 0 ? string.Empty : Extensions.FileSizeToString(size);
        }
        catch {
          return string.Empty;
        }
      }
    }

    public MediaItem CurrentMediaItem {
      get => _currentMediaItem;
      set {
        _currentMediaItem = value;
        OnPropertyChanged();

        UpdateRating();

        Dimension = _currentMediaItem == null ? string.Empty : $"{_currentMediaItem.Width}x{_currentMediaItem.Height}";
        FullGeoName = CatTreeViewUtils.GetFullPath(_currentMediaItem?.GeoName, "\n");

        OnPropertyChanged(nameof(DateAndTime));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(FileSize));
      }
    }

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < _currentMediaItem?.Rating; i++)
        Rating.Add(new AppInfoRating { IconName = IconName.Star });
    }

    private static string GetActiveFilterCountFor(BackgroundBrush bgb) {
      var count = App.Core.ActiveFilterItems.Count(x => x.BackgroundBrush == bgb);
      return count == 0 ? string.Empty : count.ToString();
    }
  }
}
