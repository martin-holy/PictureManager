using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridM : ObservableObject {
    private readonly Core _core;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    private bool _groupByFolders = true;
    private bool _groupByDate = true;
    private bool _sortByFileFirst = false;
    private bool _reWrapItems;
    private bool _scrollToTop;
    private object _scrollToItem;
    private double _thumbScale;
    private TreeWrapGroup _filteredRoot = new();

    public event EventHandler SelectionChangedEventHandler = delegate { };
    public event EventHandler FilteredChangedEventHandler = delegate { };

    public Selecting<MediaItemM> Selected { get; } = new();
    public List<MediaItemM> LoadedItems { get; } = new();
    public List<MediaItemM> FilteredItems { get; } = new();
    public MediaItemsFilterM Filter { get; } = new();
    public Func<object, int> ItemWidthGetter { get; }
    public bool ReWrapItems { get => _reWrapItems; set { _reWrapItems = value; OnPropertyChanged(); } }
    public bool ScrollToTop { get => _scrollToTop; set { _scrollToTop = value; OnPropertyChanged(); } }
    public object ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public TreeWrapGroup FilteredRoot { get => _filteredRoot; private set { _filteredRoot = value; OnPropertyChanged(); } }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public DragDropHelper.CanDragFunc CanDragFunc { get; }

    public bool GroupByFolders { get => _groupByFolders; set { _groupByFolders = value; OnPropertyChanged(); } }
    public bool GroupByDate { get => _groupByDate; set { _groupByDate = value; OnPropertyChanged(); } }
    public bool SortByFileFirst { get => _sortByFileFirst; set { _sortByFileFirst = value; OnPropertyChanged(); } }
    public string PositionSlashCount =>
      Selected.Items.Count == 0
        ? FilteredItems.Count.ToString()
        : $"{FilteredItems.IndexOf(Selected.Items[0]) + 1}/{FilteredItems.Count}";

    public bool NeedReload { get; set; }
    public double ThumbScale { get => _thumbScale; set { _thumbScale = value; OnPropertyChanged(); } }

    public RelayCommand<object> SortCommand { get; }
    public RelayCommand<object> SelectAllCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectMediaItemCommand { get; }
    public RelayCommand<MouseButtonEventArgs> OpenMediaItemCommand { get; }
    public RelayCommand<MouseWheelEventArgs> ZoomCommand { get; }

    public ThumbnailsGridM(Core core, double thumbScale, string tabTitle) {
      _core = core;
      ThumbScale = thumbScale;
      CanDragFunc = CanDrag;
      ItemWidthGetter = GetItemWidth;
      MainTabsItem = new(this, tabTitle);

      SortCommand = new(() => SoftLoad(FilteredItems, true, false));
      SelectAllCommand = new(() => Selected.Set(FilteredItems));
      ZoomCommand = new(e => Zoom(e.Delta), e => e.IsCtrlOn);

      SelectMediaItemCommand = new(e => {
        if (e.DataContext is MediaItemM mi) {
          Selected.Select(FilteredItems, mi, e.IsCtrlOn, e.IsShiftOn);
          _core.MediaItemsM.Current = Selected.Items.Contains(mi) ? mi : null;
        }
      });

      OpenMediaItemCommand = new(
        e => OpenMediaItem(e.DataContext as MediaItemM),
        e => e.ClickCount == 2);

      Filter.FilterChangedEventHandler += delegate { SoftLoad(LoadedItems, true, true); };
      Selected.ItemsChangedEventHandler += delegate { SelectionChanged(); };
      Selected.AllDeselectedEventHandler += delegate { SelectionChanged(); };
    }

    private void OpenMediaItem(MediaItemM mi) {
      if (mi == null) return;

      Selected.DeselectAll();
      _core.MainWindowM.IsFullScreen = true;
      _core.MediaViewerM.SetMediaItems(FilteredItems.ToList(), mi);
    }

    private object CanDrag(object source) {
      if (source is not MediaItemM) return null;
      var data = FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToArray();
      return data.Length == 0 ? null : data;
    }

    private int GetItemWidth(object o) {
      var width = ((MediaItemM)o).ThumbWidth;

      if (ThumbScale != ThumbnailsGridsM.DefaultThumbScale)
        width = (int)Math.Round((width / ThumbnailsGridsM.DefaultThumbScale) * ThumbScale, 0);

      return width + 6;
    }

    public void Clear() {
      foreach (var item in Selected.Items)
        item.IsSelected = false;

      Selected.Items.Clear();
      LoadedItems.Clear();
      FilteredItems.Clear();
      FilteredRoot.Items.Clear();
    }

    private void ClearItBeforeLoad() {
      ScrollToTop = true;
      Clear();
      FilteredRoot = new();
    }

    private void SelectionChanged() {
      if (this != _core.ThumbnailsGridsM.Current
        || _core.MediaViewerM.IsVisible) return;

      SelectionChangedEventHandler(this, EventArgs.Empty);
      OnPropertyChanged(nameof(PositionSlashCount));
    }

    public void UpdateSelected() {
      foreach (var mi in Selected.Items)
        mi.IsSelected = true;

      foreach (var mi in FilteredItems.Except(Selected.Items))
        mi.IsSelected = false;

      SelectionChanged();
    }

    public void Remove(List<MediaItemM> items, bool isCurrent) {
      foreach (var item in items) {
        LoadedItems.Remove(item);

        if (FilteredItems.Remove(item))
          NeedReload = true;

        if (isCurrent)
          Selected.Set(item, false);
        else
          Selected.Items.Remove(item);
      }

      if (!isCurrent) return;
      SelectionChanged();

      if (NeedReload)
        SoftLoad(FilteredItems, false, false);
    }

    private void Zoom(int delta) {
      if (delta < 0 && ThumbScale < .1) return;
      ThumbScale += delta > 0 ? .05 : -.05;
      ReWrapItems = true;
    }

    public List<MediaItemM> GetSelectedOrAll() =>
      Selected.Items.Count == 0 ? FilteredItems : Selected.Items.ToList();

    public void Shuffle() {
      LoadedItems.Shuffle();
      GroupByFolders = false;
      GroupByDate = false;
      SoftLoad(LoadedItems, true, true);
    }

    public IEnumerable<MediaItemM> Sort(IEnumerable<MediaItemM> items) =>
      SortByFileFirst
        ? items.OrderBy(x => x.FileName).ThenBy(
          x => GroupByFolders
            ? x.Folder.FolderKeyword != null
              ? x.Folder.FolderKeyword.FullPath
              : x.Folder.FullPath
            : string.Empty)
        : GroupByFolders
          ? items.OrderBy(
            x => x.Folder.FolderKeyword != null
              ? x.Folder.FolderKeyword.FullPath
              : x.Folder.FullPath).ThenBy(x => x.FileName)
          : items.OrderBy(x => x.FileName);

    private MediaItemM GetItemToScrollTo() =>
      FilteredItems.Contains(_core.MediaItemsM.Current)
        ? _core.MediaItemsM.Current
        : Selected.Items.Count != 0
          ? Selected.Items[0]
          : null;

    public void SelectAndScrollToCurrentMediaItem() {
      var mi = GetItemToScrollTo();

      Selected.DeselectAll();
      if (mi == null) return;

      Selected.Set(mi, true);
      ScrollToItem = mi;
      SelectionChanged();
    }

    public async Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
      if (!and && !hide)
        ClearItBeforeLoad();

      GroupByFolders = true;
      SortByFileFirst = false;

      var folders = _core.FoldersM.GetFolders(item, recursive);
      var bufferNew = new List<MediaItemMetadata>();

      foreach (var folder in folders) {
        if (!Directory.Exists(folder.FullPath)
          || !_core.ViewersM.CanViewerSeeContentOf(folder)) continue;

        // add MediaItems from current Folder to dictionary for faster search
        var fmis = folder.MediaItems.ToDictionary(x => x.FileName);

        foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
          if (!Imaging.IsSupportedFileType(file)) continue;

          // check if the MediaItem is already in DB, if not put it there
          var fileName = Path.GetFileName(file);
          if (!fmis.Remove(fileName, out var mi)) {
            mi = _core.MediaItemsM.AddNew(folder, fileName);
            var mim = new MediaItemMetadata(mi);

            if (mi.MediaType == MediaType.Video) {
              await LoadByFolderProcessBuffer(bufferNew, MediaType.Image, and, hide);
              await LoadByFolderProcessBuffer(new() { mim }, MediaType.Video, and, hide);
            }
            else if (mi.MediaType == MediaType.Image) {
              bufferNew.Add(mim);
              if (bufferNew.Count > 15)
                await LoadByFolderProcessBuffer(bufferNew, MediaType.Image, and, hide);
            }
          }
          else {
            await LoadByFolderProcessBuffer(bufferNew, MediaType.Image, and, hide);
            AddMediaItem(mi, and, hide);
          }
        }

        await LoadByFolderProcessBuffer(bufferNew, MediaType.Image, and, hide);

        // remove MediaItems deleted outside of this application
        foreach (var fmi in fmis.Values)
          _core.MediaItemsM.Delete(fmi);
      }

      if (and || hide)
        SoftLoad(FilteredItems, true, false);
      else
        AfterLoad(true);
    }

    private async Task LoadByFolderProcessBuffer(List<MediaItemMetadata> buffer, MediaType type, bool and, bool hide) {
      if (buffer.Count == 0) return;

      if (type == MediaType.Image)
        await Task.Run(() =>
          Parallel.ForEach(
            buffer,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            mim => _core.MediaItemsM.ReadMetadata(mim, false)));
      else if (type == MediaType.Video)
        foreach (var mim in buffer)
          _core.MediaItemsM.ReadMetadata(mim, false);

      foreach (var mim in buffer) {
        if (mim.Success) {
          mim.FindRefs(_core);
          AddMediaItem(mim.MediaItem, and, hide);
        }
        else
          _core.MediaItemsM.Delete(mim.MediaItem);
      }

      buffer.Clear();
    }

    private void AddMediaItem(MediaItemM mi, bool and = false, bool hide = false) {
      if (hide) {
        LoadedItems.Remove(mi);
        FilteredItems.Remove(mi);
        if (Selected.Items.Remove(mi))
          mi.IsSelected = false;

        return;
      }

      if (and && LoadedItems.Contains(mi)) return;
      if (!_core.ViewersM.CanViewerSee(mi)) return;

      mi.SetThumbSize();
      mi.SetInfoBox();
      LoadedItems.Add(mi);

      if (Filter.Filter(mi)) {
        FilteredItems.Add(mi);
        AddMediaItemToGrid(mi);
      }
    }

    private void AddMediaItemToGrid(MediaItemM mi) {
      TreeWrapGroup group = new() { IsExpanded = true };

      if (GroupByFolders) {
        var folderName = mi.Folder.Name;
        var iOfL = folderName.FirstIndexOfLetter();
        var title = iOfL == 0 || folderName.Length - 1 == iOfL ? folderName : folderName[iOfL..];
        var toolTip = mi.Folder.FolderKeyword != null
          ? mi.Folder.FolderKeyword.FullPath
          : mi.Folder.FullPath;
        group.Info.Add(new(Res.IconFolder, title, toolTip));
      }

      if (GroupByDate) {
        var title = DateTimeExtensions.DateTimeFromString(mi.FileName, _dateFormats, null);
        if (!string.IsNullOrEmpty(title))
          group.Info.Add(new(Res.IconCalendar, title));
      }

      var lastGroup = FilteredRoot.Items.LastOrDefault() as TreeWrapGroup;
      var groupA = group.Info.ToArray();
      var groupB = lastGroup?.Info.ToArray();

      if (lastGroup != null && TreeWrapGroupInfoItem.AreEqual(groupA, groupB))
        group = lastGroup;
      else
        FilteredRoot.Items.Add(group);

      group.Items.Add(mi);
    }

    public void SoftLoad(IEnumerable<MediaItemM> items, bool sort, bool filter) {
      if (Selected.Items.Count > 0)
        ScrollToTop = true;

      IEnumerable<MediaItemM> toLoad = items.ToArray();
      FilteredItems.Clear();
      FilteredRoot.Items.Clear();
      FilteredRoot = new();

      toLoad = filter
        ? toLoad.Where(Filter.Filter)
        : toLoad;

      toLoad = sort
        ? Sort(toLoad)
        : toLoad;

      FilteredItems.AddRange(toLoad);

      foreach (var mi in toLoad)
        AddMediaItemToGrid(mi);

      AfterLoad(false);
    }

    public async Task LoadByTag(IEnumerable<MediaItemM> itemsToLoad) {
      ClearItBeforeLoad();

      var items = Sort(itemsToLoad).ToArray();

      // check if folders are available
      var foldersSet = await Task.Run(() => {
        var folders = items.Select(x => x.Folder).Distinct();
        var set = new HashSet<FolderM>();

        foreach (var folder in folders) {
          if (!_core.ViewersM.CanViewerSeeContentOf(folder)) continue;
          if (!Directory.Exists(folder.FullPath)) continue;
          set.Add(folder);
        }

        return set;
      });

      // check if files are available
      var skip = new HashSet<MediaItemM>();
      const int buffer = 50;

      for (int i = 0; i < (items.Length / buffer) + 1; i++) {
        await Task.Run(() => {
          for (int j = i * buffer; j < (i + 1) * buffer && j < items.Length; j++) {
            var mi = items[j];
            if (!foldersSet.Contains(mi.Folder) || !File.Exists(mi.FilePath)) {
              skip.Add(mi);
              continue;
            }
          }
        });

        for (int j = i * buffer; j < (i + 1) * buffer && j < items.Length; j++) {
          var mi = items[j];
          if (skip.Remove(mi)) continue;
          AddMediaItem(mi);
        }
      }

      AfterLoad(true);
    }

    private void AfterLoad(bool maxSizeSelection) {
      foreach (var group in FilteredRoot.Items.OfType<TreeWrapGroup>()) {
        var info = group.Info.SingleOrDefault(x => x.Icon.Equals(Res.IconImageMultiple));
        var title = group.Items.Count.ToString();
        if (info == null)
          group.Info.Add(new(Res.IconImageMultiple, title));
        else
          info.Title = title;
      }

      foreach (var mi in Selected.Items.Where(x => !FilteredItems.Contains(x)).ToArray())
        Selected.Set(mi, false);

      OnPropertyChanged(nameof(PositionSlashCount));
      FilteredChangedEventHandler(this, EventArgs.Empty);
      NeedReload = false;
      Filter.UpdateSizeRanges(LoadedItems, maxSizeSelection);

      if (FilteredItems.Contains(_core.MediaItemsM.Current))
        ScrollToItem = _core.MediaItemsM.Current;
      else {
        _core.MediaItemsM.Current = null;
        if (Selected.Items.Count != 0)
          ScrollToItem = Selected.Items[0];
      }
    }
  }
}
