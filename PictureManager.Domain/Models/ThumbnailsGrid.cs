using PictureManager.Domain.CatTreeViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models {
  // On Tab Activate
  // poresit ThumbSize, pokud bude rozdilna v tabech


  // filter by mel bejt taky asi na tab

  public class ThumbnailsGrid : ObservableObject {
    private MediaItem _current;
    private int? _indexOfCurrent;
    private int _selected;
    private string _title;
    private bool _showImages = true;
    private bool _showVideos = true;
    private bool _groupByFolders = true;
    private bool _groupByDate = true;
    private bool _sortByFileFirst = true;

    public List<MediaItem> SelectedItems { get; } = new();
    public List<MediaItem> LoadedItems { get; } = new();
    public ObservableCollection<MediaItem> FilteredItems { get; } = new();
    public ObservableCollection<object> Rows { get; } = new();
    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";
    public bool ShowImages { get => _showImages; set { _showImages = value; OnPropertyChanged(); } }
    public bool ShowVideos { get => _showVideos; set { _showVideos = value; OnPropertyChanged(); } }
    public bool GroupByFolders { get => _groupByFolders; set { _groupByFolders = value; OnPropertyChanged(); } }
    public bool GroupByDate { get => _groupByDate; set { _groupByDate = value; OnPropertyChanged(); } }
    public bool SortByFileFirst { get => _sortByFileFirst; set { _sortByFileFirst = value; OnPropertyChanged(); } }
    public delegate Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent);

    public MediaItem Current {
      get => _current;
      set {
        if (_current != null) SetSelected(_current, false);
        _current = value;
        if (_current != null) SetSelected(_current, true);
        _indexOfCurrent = value == null ? null : FilteredItems.IndexOf(value);

        // temporary
        if (Core.Instance.MediaItems.Current != value)
          Core.Instance.MediaItems.Current = value;

        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));
      }
    }

    public void ClearItBeforeLoad() {
      Current = null;
      foreach (var item in LoadedItems) {
        SetSelected(item, false);
        item.InfoBoxThumb = null;
        item.InfoBoxPeople = null;
        item.InfoBoxKeywords = null;
      }

      SelectedItems.Clear();
      LoadedItems.Clear();
      FilteredItems.Clear();
    }

    public void SetSelected(MediaItem mi, bool value) {
      if (mi.IsSelected == value) return;
      mi.IsSelected = value;
      if (value) SelectedItems.Add(mi);
      else SelectedItems.Remove(mi);
      Selected = SelectedItems.Count;
    }

    public void UpdateSelected() {
      foreach (var mi in SelectedItems)
        mi.IsSelected = true;

      foreach (var mi in FilteredItems.Except(SelectedItems))
        mi.IsSelected = false;

      Selected = SelectedItems.Count;
    }

    public void Select(bool isCtrlOn, bool isShiftOn, MediaItem mi) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();
        Current = mi;
        return;
      }

      // single invert select
      if (isCtrlOn)
        SetSelected(mi, !mi.IsSelected);

      // multi select
      if (isShiftOn && Current != null && _indexOfCurrent != null) {
        var from = (int)_indexOfCurrent;
        var indexOfMi = FilteredItems.IndexOf(mi);
        var to = indexOfMi;
        if (from > to) {
          to = from;
          from = indexOfMi;
        }

        for (var i = from; i < to + 1; i++) {
          SetSelected(FilteredItems[i], true);
        }
      }

      // 
      if (Selected == 0)
        Current = null;
      else if (Selected > 1) {
        var isCurrentSelected = Current?.IsSelected ?? false;
        var current = Current;
        Current = null;
        if (isCurrentSelected)
          SetSelected(current, true);
      }
    }

    public void DeselectAll() {
      Current = null;
      foreach (var mi in LoadedItems)
        SetSelected(mi, false);
    }

    public void SelectAll() {
      Current = null;
      foreach (var mi in FilteredItems)
        SetSelected(mi, true);
    }

    public void Remove(List<MediaItem> items, bool delete, FileOperationDelete fileOperationDelete) {
      if (items.Count == 0) return;

      // set Current to next MediaItem after last selected or one before first selected or null
      var indexOfNewCurrent = FilteredItems.IndexOf(items[^1]) + 1;
      if (indexOfNewCurrent == FilteredItems.Count)
        indexOfNewCurrent = FilteredItems.IndexOf(items[0]) - 1;
      Current = indexOfNewCurrent >= 0 ? FilteredItems[indexOfNewCurrent] : null;

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var mi in items) {
        LoadedItems.Remove(mi);
        FilteredItems.Remove(mi);
        if (delete) {
          files.Add(mi.FilePath);
          cache.Add(mi.FilePathCache);
          Core.Instance.MediaItems.Delete(mi);
        }
        else SetSelected(mi, false);
      }

      // update Current after the FilteredItems were changed
      OnPropertyChanged(nameof(Current));

      if (delete) {
        fileOperationDelete.Invoke(files, true, false);
        cache.ForEach(File.Delete);
      }
    }

    public void RemoveSelected(bool delete, FileOperationDelete fileOperationDelete) =>
      Remove(FilteredItems.Where(x => x.IsSelected).ToList(), delete, fileOperationDelete);

    public void ResetThumbsSize() {
      foreach (var item in LoadedItems)
        item.SetThumbSize(true);
    }

    public List<MediaItem> GetSelectedOrAll() => SelectedItems.Count == 0 ? FilteredItems.ToList() : SelectedItems;

    public void SelectNotModified() {
      Current = null;
      foreach (var mi in FilteredItems) {
        SetSelected(mi, false);
        if (!mi.IsModified)
          SetSelected(mi, true);
      }
    }

    public MediaItem GetNext() {
      if (Current == null || _indexOfCurrent == null || FilteredItems.Count <= _indexOfCurrent + 1) return null;

      return FilteredItems[(int)_indexOfCurrent + 1];
    }

    public MediaItem GetPrevious() {
      if (Current == null || _indexOfCurrent == null || _indexOfCurrent < 1) return null;

      return FilteredItems[(int)_indexOfCurrent - 1];
    }

    public void FilteredItemsSetInPlace(MediaItem mi) {
      var oldIndex = FilteredItems.IndexOf(mi);
      var newIndex = FilteredItems.OrderBy(x => x.FileName).ToList().IndexOf(mi);
      FilteredItems.Move(oldIndex, newIndex);
    }

    public void ReloadFilteredItems() {
      FilteredItems.Clear();

      var filtered = MediaItems.Filter(LoadedItems);

      var sorted = SortByFileFirst
        ? filtered.OrderBy(x => x.FileName).ThenBy(
          x => GroupByFolders
            ? x.Folder.FolderKeyword != null
              ? CatTreeViewUtils.GetFullPath(x.Folder.FolderKeyword, Path.DirectorySeparatorChar.ToString())
              : x.Folder.FullPath
            : string.Empty)
        : GroupByFolders
          ? filtered.OrderBy(
            x => x.Folder.FolderKeyword != null
              ? CatTreeViewUtils.GetFullPath(x.Folder.FolderKeyword, Path.DirectorySeparatorChar.ToString())
              : x.Folder.FullPath).ThenBy(x => x.FileName)
          : filtered.OrderBy(x => x.FileName);

      foreach (var mi in sorted)
        FilteredItems.Add(mi);

      if (FilteredItems.IndexOf(Current) < 0)
        Core.Instance.RunOnUiThread(() => { Current = null; });

      OnPropertyChanged(nameof(PositionSlashCount));
    }
  }
}
