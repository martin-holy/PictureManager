using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.Domain.Models {
  // On Tab Activate
  // poresit ThumbSize, pokud bude rozdilna v tabech


  // filter by mel bejt taky asi na tab

  public class ThumbnailsGrid: ObservableObject {
    private MediaItem _current;
    private int? _indexOfCurrent;
    private int _selected;
    private string _title;
    private bool _showImages = true;
    private bool _showVideos = true;
    private bool _groupByFolders = true;
    private bool _groupByDate = true;
    private readonly Dictionary<string, string> _dateFormats = new Dictionary<string, string>{{"d", "d. "}, {"M", "MMMM "}, {"y", "yyyy"}};

    public List<MediaItem> SelectedItems { get; } = new List<MediaItem>();
    public List<MediaItem> LoadedItems { get; } = new List<MediaItem>();
    public ObservableCollection<MediaItem> FilteredItems { get; } = new ObservableCollection<MediaItem>();
    public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();
    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } } 
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";
    public bool ShowImages { get => _showImages; set { _showImages = value; OnPropertyChanged(); } }
    public bool ShowVideos { get => _showVideos; set { _showVideos = value; OnPropertyChanged(); } }
    public bool GroupByFolders { get => _groupByFolders; set { _groupByFolders = value; OnPropertyChanged(); } }
    public bool GroupByDate { get => _groupByDate; set { _groupByDate = value; OnPropertyChanged(); } }
    public delegate Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent);

    public MediaItem Current {
      get => _current;
      set {
        if (_current != null) SetSelected(_current, false);
        _current = value;
        if (_current != null) SetSelected(_current, true);
        _indexOfCurrent = value == null ? null : (int?)FilteredItems.IndexOf(value);
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
      ClearRows();
    }

    public void ClearRows() {
      foreach (var row in Rows.OfType<MediaItemsRow>())
        row.Items.Clear();

      Rows.Clear();
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
      var indexOfNewCurrent = FilteredItems.IndexOf(items[items.Count - 1]) + 1;
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
      Current = Current;

      if (delete) {
        fileOperationDelete.Invoke(files, true, false);
        cache.ForEach(File.Delete);
      }
    }

    public void RemoveSelected(bool delete, FileOperationDelete fileOperationDelete) {
      Remove(FilteredItems.Where(x => x.IsSelected).ToList(), delete, fileOperationDelete);
    }

    private string GetFolderTitle(string folderName) {
      if (!GroupByFolders) return string.Empty;
      var iOfL = folderName.FirstIndexOfLetter();
      return iOfL == 0 || folderName.Length - 1 == iOfL ? folderName : folderName.Substring(iOfL);
    }

    private void AddGroup(MediaItem mi) {
      var group = Rows.OfType<MediaItemsGroup>().LastOrDefault();
      var miFolderTitle = GetFolderTitle(mi.Folder.Title);
      var miFolderFullPath = mi.Folder.FolderKeyword != null
        ? CatTreeViewUtils.GetFullPath(mi.Folder.FolderKeyword, Path.DirectorySeparatorChar.ToString())
        : mi.Folder.FullPath;
      var miDate = !GroupByDate ? string.Empty : Extensions.DateTimeFromString(mi.FileName, _dateFormats, null);

      if (group == null || !group.Date.Equals(miDate) && GroupByDate || !group.FolderFullPath.Equals(miFolderFullPath) && GroupByFolders) {
        group = new MediaItemsGroup {Date = miDate, Folder = miFolderTitle, FolderFullPath = miFolderFullPath};
        Rows.Add(group);
        Rows.Add(new MediaItemsRow());
      }

      group.ItemsCount++;
    }

    public void AddItem(MediaItem mi, double rowMaxWidth) {
      // Add Media Items Group
      AddGroup(mi);

      // Add Media Items Row
      var row = Rows.OfType<MediaItemsRow>().LastOrDefault();
      if (row == null) {
        row = new MediaItemsRow();
        Rows.Add(row);
      }

      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      var rowWidth = row.Items.Sum(x => x.ThumbWidth + itemOffset);

      if (row.Items.Count > 0 && mi.ThumbWidth + itemOffset > rowMaxWidth - rowWidth) {
        row = new MediaItemsRow();
        Rows.Add(row);
      }

      // Add Media Item
      row.Items.Add(mi);
    }

    public int GetRowIndexWith(MediaItem mi) {
      var rowIndex = 0;
      foreach (var row in Rows) {
        if (row is MediaItemsRow itemsRow)
          if (itemsRow.Items.Any(x => x.Id.Equals(mi.Id)))
            break;
        rowIndex++;
      }

      return rowIndex;
    }

    public void ResetThumbsSize() {
      foreach (var item in LoadedItems)
        item.SetThumbSize(true);
    }

    public List<MediaItem> GetSelectedOrAll() {
      return SelectedItems.Count == 0 ? FilteredItems.ToList() : SelectedItems;
    }

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

      var sorted = GroupByFolders
        ? filtered.OrderBy(x =>
          x.Folder.FolderKeyword != null
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
