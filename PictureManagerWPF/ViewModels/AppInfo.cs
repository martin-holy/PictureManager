using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class AppInfoRating {
    public IconName IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _progressBarValueA;
    private int _progressBarValueB;
    private bool _progressBarIsIndeterminate;
    private MediaItem _currentMediaItem;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;
    private string _dimension = string.Empty;
    private string _fullGeoName = string.Empty;

    public int ProgressBarValueA { get => _progressBarValueA; set { _progressBarValueA = value; OnPropertyChanged(); } }
    public int ProgressBarValueB { get => _progressBarValueB; set { _progressBarValueB = value; OnPropertyChanged(); } }
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }
    public string Dimension { get => _dimension; set { _dimension = value; OnPropertyChanged(); } }
    public string ZoomActualFormatted => App.WMain?.FullImage.ZoomActualFormatted;
    public string FullGeoName { get => _fullGeoName; set { _fullGeoName = value; OnPropertyChanged(); } }
    public ObservableCollection<AppInfoRating> Rating { get; } = new ObservableCollection<AppInfoRating>();

    public bool IsGeoNameVisible => CurrentMediaItem?.GeoName != null;
    public bool IsCommentVisible => AppMode == AppMode.Viewer && !string.IsNullOrEmpty(CurrentMediaItem?.Comment);
    public bool IsInfoBoxPeopleVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.InfoBoxPeople != null;
    public bool IsInfoBoxKeywordsVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.InfoBoxKeywords != null;
    public bool IsImageActualZoomVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.MediaType == MediaType.Image;
    public bool IsSelectedCountVisible => AppMode == AppMode.Browser;

    public string FilterAndCount => GetActiveFilterCountFor(BackgroundBrush.AndThis);
    public string FilterOrCount => GetActiveFilterCountFor(BackgroundBrush.OrThis);
    public string FilterHiddenCount => GetActiveFilterCountFor(BackgroundBrush.Hidden);

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public AppMode AppMode {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsCommentVisible));
        OnPropertyChanged(nameof(IsInfoBoxPeopleVisible));
        OnPropertyChanged(nameof(IsInfoBoxKeywordsVisible));
        OnPropertyChanged(nameof(IsImageActualZoomVisible));
        OnPropertyChanged(nameof(IsSelectedCountVisible));
        OnPropertyChanged(nameof(FilePath));
        App.WMain.SetFlyoutMainTreeViewMargin();
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

        var fks = new List<BaseTreeViewItem>();
        CurrentMediaItem.Folder.FolderKeyword.GetThisAndParentRecursive(ref fks);
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Title.FirstIndexOfLetter();

            if (fk.Title.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Title : fk.Title.Substring(startIndex));
          }

        var fileName = string.IsNullOrEmpty(DateAndTime)
          ? CurrentMediaItem.FileName
          : CurrentMediaItem.FileName.Substring(15);
        paths.Add(fileName);

        return paths;
      }
    }

    public string FileSize {
      get {
        try {
          if (CurrentMediaItem == null)
            return string.Empty;

          var fi = new FileInfo(CurrentMediaItem.FilePath);
          return Extensions.FileSizeToString(fi.Length);
        }
        catch {
          return string.Empty;
        }
      }
    }

    public string DateAndTime {
      get {
        var sdt = CurrentMediaItem?.FileName.Length < 15 ? string.Empty : CurrentMediaItem?.FileName.Substring(0, 15);
        var success = DateTime.TryParseExact(sdt, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt);

        return success ? dt.ToString("d. MMMM yyyy, H:mm:ss", CultureInfo.CurrentCulture) : string.Empty;
      }
    }

    public MediaItem CurrentMediaItem {
      get => _currentMediaItem;
      set {
        _currentMediaItem = value;
        OnPropertyChanged();

        UpdateRating();

        Dimension = _currentMediaItem == null ? string.Empty : $"{_currentMediaItem.Width}x{_currentMediaItem.Height}";
        FullGeoName = _currentMediaItem?.GeoName?.GetFullPath("\n");
        
        OnPropertyChanged(nameof(IsGeoNameVisible));
        OnPropertyChanged(nameof(IsCommentVisible));
        OnPropertyChanged(nameof(IsInfoBoxPeopleVisible));
        OnPropertyChanged(nameof(IsInfoBoxKeywordsVisible));
        OnPropertyChanged(nameof(IsImageActualZoomVisible));
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
      var count = App.Core.Model.ActiveFilterItems.Count(x => x.BackgroundBrush == bgb);
      return count == 0 ? string.Empty : count.ToString();
    }
  }
}
