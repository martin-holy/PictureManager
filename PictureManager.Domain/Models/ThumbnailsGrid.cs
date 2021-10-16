using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGrid : ObservableObject {
    private MediaItem _current;
    private int? _indexOfCurrent;
    private List<MediaItem> _selectedItems = new();
    private string _title;
    private bool _showImages = true;
    private bool _showVideos = true;
    private bool _groupByFolders = true;
    private bool _groupByDate = true;
    private bool _sortByFileFirst = true;

    public EventHandler OnSelectionChanged { get; set; }

    public List<MediaItem> SelectedItems => _selectedItems;
    public List<MediaItem> LoadedItems { get; } = new();
    public List<MediaItem> FilteredItems { get; } = new();
    public int SelectedCount => SelectedItems.Count;
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public bool ShowImages { get => _showImages; set { _showImages = value; OnPropertyChanged(); } }
    public bool ShowVideos { get => _showVideos; set { _showVideos = value; OnPropertyChanged(); } }
    public bool GroupByFolders { get => _groupByFolders; set { _groupByFolders = value; OnPropertyChanged(); } }
    public bool GroupByDate { get => _groupByDate; set { _groupByDate = value; OnPropertyChanged(); } }
    public bool SortByFileFirst { get => _sortByFileFirst; set { _sortByFileFirst = value; OnPropertyChanged(); } }
    public bool NeedReload { get; set; }

    public MediaItem Current {
      get => _current;
      set {
        _current = value;
        _indexOfCurrent = value == null ? null : FilteredItems.IndexOf(value);

        // TODO temporary
        if (Core.Instance.MediaItems.Current != value)
          Core.Instance.MediaItems.Current = value;

        OnPropertyChanged();
        UpdatePositionSlashCount();
      }
    }

    private void UpdatePositionSlashCount() =>
      Core.Instance.MediaItems.PositionSlashCount = $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";

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

    private void SelectionChanged() {
      OnSelectionChanged?.Invoke(this, EventArgs.Empty);
      OnPropertyChanged(nameof(SelectedCount));
    }

    public void SetSelected(MediaItem mi, bool value) => Selecting.SetSelected(_selectedItems, mi, value, () => SelectionChanged());

    public void UpdateSelected() {
      foreach (var mi in SelectedItems)
        mi.IsSelected = true;

      foreach (var mi in FilteredItems.Except(SelectedItems))
        mi.IsSelected = false;

      Current = SelectedItems.Count == 1 ? SelectedItems[0] : null;
      SelectionChanged();
    }

    public void Select(MediaItem mi, bool isCtrlOn, bool isShiftOn) {
      Selecting.Select(_selectedItems, FilteredItems, mi, isCtrlOn, isShiftOn, () => SelectionChanged());
      Current = SelectedItems.Count == 1 ? SelectedItems[0] : null;
    }

    public void DeselectAll() {
      Current = null;
      foreach (var mi in SelectedItems.ToArray())
        SetSelected(mi, false);
    }

    public void SelectAll() {
      Current = null;
      foreach (var mi in FilteredItems)
        SetSelected(mi, true);
    }

    public void Remove(List<MediaItem> items) {
      Current = MediaItems.GetNewCurrent(FilteredItems, items);

      foreach (var mi in items)
        Remove(mi);
    }

    public void Remove(MediaItem item) {
      SetSelected(item, false);
      if (item == Current)
        Current = null;
      LoadedItems.Remove(item);
      if (FilteredItems.Remove(item))
        NeedReload = true;
    }

    public void RemoveSelected() => Remove(FilteredItems.Where(x => x.IsSelected).ToList());

    public void ResetThumbsSize() {
      foreach (var item in LoadedItems)
        item.SetThumbSize(true);
    }

    public List<MediaItem> GetSelectedOrAll() => SelectedItems.Count == 0 ? FilteredItems : SelectedItems;

    public void SelectNotModified() {
      foreach (var mi in FilteredItems)
        SetSelected(mi, !mi.IsModified);

      Current = null;
    }

    public void FilteredItemsSetInPlace(MediaItem mi) {
      var oldIndex = FilteredItems.IndexOf(mi);
      var newIndex = FilteredItems.OrderBy(x => x.FileName).ToList().IndexOf(mi);
      FilteredItems.RemoveAt(oldIndex);
      FilteredItems.Insert(newIndex, mi);
    }

    public async Task ReloadFilteredItems(IEnumerable<MediaItem> filtered) {
      FilteredItems.Clear();

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
        await Core.Instance.RunOnUiThread(() => Current = null);

      UpdatePositionSlashCount();
    }
  }
}
