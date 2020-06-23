using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models {
  // On Tab Activate
  // poresit ThumbSize, pokud bude rozdilna v tabech


  // filter by mel bejt taky asi na tab

  public class ThumbnailsGrid: ObservableObject {
    private MediaItem _current;
    private int? _indexOfCurrent;
    private int _selected;
    private string _title;
    private bool _showAddTabButton;

    public List<MediaItem> SelectedItems { get; } = new List<MediaItem>();
    public List<MediaItem> LoadedItems { get; } = new List<MediaItem>();
    public ObservableCollection<MediaItem> FilteredItems { get; } = new ObservableCollection<MediaItem>();
    public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();
    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } } 
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";
    public bool ShowAddTabButton { get => _showAddTabButton; set { _showAddTabButton = value; OnPropertyChanged(); } }
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

    public bool AddGroup(MediaItem mi) {
      var group = Rows.OfType<MediaItemsGroup>().LastOrDefault();

      // tady k dohledany skupine pridavat postupne pocet souboru, ...

      // Add Folder Group
      //if (group != null && group.Folder.Equals(mi.Folder)) return false;
      //SplittedItems.Add(new MediaItemsGroup { Title = mi.Folder.FullPath, Folder = mi.Folder });

      // Add Date Group
      var miDate = MediaItem.GetDateTimeFromName(mi, "d. MMMM yyyy");
      if (string.Empty.Equals(miDate)) return false;
      if (group != null && group.Title.Equals(miDate)) return false;
      Rows.Add(new MediaItemsGroup { Title = miDate });

      return true;
    }

    public void AddItem(MediaItem mi, double rowMaxWidth) {
      // Add Media Items Group
      if (AddGroup(mi)) {
        Rows.Add(new MediaItemsRow { Items = { mi } });
        return;
      }

      // Add Media Items Row
      var row = Rows.OfType<MediaItemsRow>().LastOrDefault();
      if (row == null) {
        row = new MediaItemsRow();
        Rows.Add(row);
      }

      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      var rowWidth = row.Items.Sum(x => x.ThumbWidth + itemOffset);

      if (mi.ThumbWidth + itemOffset > rowMaxWidth - rowWidth) {
        row = new MediaItemsRow();
        Rows.Add(row);
      }

      // Add Media Item
      row.Items.Add(mi);
    }

    public void ReloadItems(double rowMaxWidth, bool withGroups) {
      ClearRows();

      const int itemOffset = 6; //border, margin, padding, ...
      MediaItemsGroup group = null;
      MediaItemsRow row = null;
      var rowWidth = 0;

      foreach (var mi in FilteredItems) {
        // Add Date Group
        if (withGroups) {
          var miDate = MediaItem.GetDateTimeFromName(mi, "d. MMMM yyyy");
          if (!string.Empty.Equals(miDate) && (group == null || !group.Title.Equals(miDate))) {
            group = new MediaItemsGroup {Title = miDate};
            row = new MediaItemsRow();
            Rows.Add(group);
            Rows.Add(row);
            rowWidth = 0;
          }
        }

        if (row == null || mi.ThumbWidth + itemOffset > rowMaxWidth - rowWidth) {
          rowWidth = 0;
          row = new MediaItemsRow();
          Rows.Add(row);
        }

        row.Items.Add(mi);
        rowWidth += mi.ThumbWidth + itemOffset;
      }
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

    public void ReapplyFilter() {
      Current = null;
      FilteredItems.Clear();

      foreach (var mi in MediaItems.Filter(LoadedItems)) {
        mi.SetInfoBox();
        FilteredItems.Add(mi);
      }

      OnPropertyChanged(nameof(PositionSlashCount));
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
  }
}
