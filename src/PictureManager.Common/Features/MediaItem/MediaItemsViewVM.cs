using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public class MediaItemsViewVM : MediaItemCollectionView {
  private bool _isLoading;
  private bool _showThumbInfo = true;
  
  public event EventHandler SelectionChangedEventHandler = delegate { };
  public event EventHandler FilteredChangedEventHandler = delegate { };

  public List<MediaItemM> LoadedItems { get; } = [];
  public MediaItemsFilterVM Filter { get; } = new();
  public MediaItemsImport Import { get; } = new();
  public DragDropHelper.CanDragFunc CanDragFunc { get; }
  public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
  public bool ShowThumbInfo { get => _showThumbInfo; set { _showThumbInfo = value; OnPropertyChanged(); } }
  
  public string PositionSlashCount =>
    Selected.Items.Count == 0
      ? Root.Source.Count.ToString()
      : $"{Root.Source.IndexOf(Selected.Items[0]) + 1}/{Root.Source.Count}";

  public RelayCommand SelectAllCommand { get; }

  public MediaItemsViewVM(double thumbScale) : base(thumbScale) {
    CanDragFunc = _canDrag;
    SetFilter(Filter);
    SelectAllCommand = new(() => Selected.Set(Root.Source));
    Filter.FilterChangedEvent += delegate { SoftLoad(LoadedItems, true, true); };
    Selected.ItemsChangedEvent += delegate { _selectionChanged(); };
    Selected.AllDeselectedEvent += delegate { _selectionChanged(); };
    PropertyChanged += _onPropertyChanged;
  }

  private void _onPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(ThumbScale)))
      ShowThumbInfo = ThumbScale > 0.4;
  }

  public override void OnIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem, Core.Settings.MediaItem.ScrollExactlyToMediaItem);
  }

  public override void OnItemOpened(MediaItemM item) {
    Selected.DeselectAll();

    if (LastSelectedRow?.Parent is not CollectionViewGroup<MediaItemM> group) return;
    Core.VM.MainWindow.IsInViewMode = true;
    Core.VM.MediaViewer.SetMediaItems(group.Source.ToList(), item);
  }

  public override void OnItemSelected(SelectionEventArgs<MediaItemM> e) {
    base.OnItemSelected(e);
    Core.VM.MediaItem.Current = Selected.Items.Contains(e.Item) ? e.Item : null;
  }

  private object? _canDrag(object source) {
    if (source is not MediaItemM) return null;
    var data = Selected.Items.OfType<RealMediaItemM>().Select(p => p.FilePath).ToArray();
    return data.Length == 0 ? null : data;
  }

  public void Clear() {
    foreach (var item in Selected.Items)
      item.IsSelected = false;

    Selected.Items.Clear();
    LoadedItems.Clear();
  }

  private void _selectionChanged() {
    if (!ReferenceEquals(this, Core.VM.MediaItem.Views.Current) || Core.VM.MediaViewer.IsVisible) return;

    SelectionChangedEventHandler(this, EventArgs.Empty);
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  public void UpdateSelected() {
    foreach (var mi in Selected.Items)
      mi.IsSelected = true;

    foreach (var mi in Root.Source.Except(Selected.Items).Where(x => x.IsSelected))
      mi.IsSelected = false;

    _selectionChanged();
  }

  public void Remove(IList<MediaItemM> items, bool isCurrent) {
    foreach (var item in items) {
      LoadedItems.Remove(item);

      if (isCurrent)
        Selected.Set(item, false);
      else
        Selected.Items.Remove(item);
    }

    Remove(items.ToArray());
    _selectionChanged();
  }

  public List<MediaItemM> GetSelectedOrAll() =>
    Selected.Items.Count == 0 ? Root.Source : Selected.Items.ToList();

  public void Shuffle() {
    LoadedItems.Shuffle();
    SoftLoad(LoadedItems, false, true);
  }

  public void Sort() =>
    SoftLoad(LoadedItems, true, true);

  public static IEnumerable<MediaItemM> GetSorted(IEnumerable<MediaItemM> items) =>
    items.OrderBy(x => x.FileName);

  private MediaItemM? _getItemToScrollTo() =>
    Core.VM.MediaItem.Current is { } current && Root.Source.Contains(current)
      ? current
      : Selected.Items.FirstOrDefault();

  public void SelectAndScrollToCurrentMediaItem() {
    var mi = _getItemToScrollTo();
    Core.VM.MediaItem.Current = mi;
    if (mi == null) return;
    Selected.Set(mi, true);
    var group = LastSelectedRow?.Parent as CollectionViewGroup<MediaItemM> ?? Root;
    ScrollTo(group, mi, Core.Settings.MediaItem.ScrollExactlyToMediaItem);
    _selectionChanged();
  }

  public Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
    IsLoading = true;
    if (!and && !hide)
      Clear();

    var folders = Core.S.Folder.GetFolders(item, recursive);
    var newItems = new List<MediaItemMetadata>();
    var toProcess = new List<MediaItemM>();

    foreach (var folder in folders) {
      // TODO do this check in Core.S.Folder.GetFolders
      if (!Directory.Exists(folder.FullPath)
          || !Core.S.Viewer.CanViewerSeeContentOf(folder)) continue;

      // add MediaItems from current Folder to dictionary for faster search
      var mediaItems = folder.MediaItems.ToDictionary(x => x.FileName);

      // get new items from folder
      foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
        var fileName = Path.GetFileName(file);
        if (mediaItems.Remove(fileName)) continue;
        if (Core.R.MediaItem.ItemCreate(folder, fileName) is not { } mi) continue;
        newItems.Add(new(mi));
      }

      // remove MediaItems deleted outside of this application
      Core.R.MediaItem.ItemsDelete(mediaItems.Values.Cast<MediaItemM>().ToArray());

      toProcess.AddRange(folder.MediaItems);
    }

    return Import.Import(newItems).ContinueWith(delegate {
      var notImported = newItems.Where(x => !x.Success).Select(x => x.MediaItem);
      toProcess = toProcess.Except(notImported).ToList();

      Selected.DeselectAll();
      foreach (var mi in toProcess.Where(mi => mi.IsSelected))
        mi.IsSelected = false;

      if (hide) {
        Remove(toProcess.ToArray());
      }
      else {
        toProcess = toProcess.Where(Core.S.Viewer.CanViewerSee).ToList();

        foreach (var mi in toProcess) {
          mi.SetThumbSize();
          mi.SetInfoBox();
        }

        if (and) {
          Insert(toProcess.ToArray());
        }
        else {
          Reload(GetSorted(toProcess).ToList(), GroupMode.ThenByRecursive, null, true);

          // TODO is this necessary?
          if (newItems.Count > 0) CollectionViewGroup<MediaItemM>.ReWrapAll(Root);
        }
      }

      Filter.UpdateSizeRanges(GetUnfilteredItems().ToArray());
      
      _afterLoad();
      IsLoading = false;
    }, Tasks.UiTaskScheduler);
  }

  private List<MediaItemM> _addMediaItems(List<MediaItemM> items, bool and = false, bool hide = false) {
    Selected.DeselectAll();
    foreach (var mi in items) {
      if (mi.IsSelected) mi.IsSelected = false;

      if (hide) {
        LoadedItems.Remove(mi);
        continue;
      }

      if (and && LoadedItems.Contains(mi)) continue;
      if (!Core.S.Viewer.CanViewerSee(mi)) continue;

      mi.SetThumbSize();
      mi.SetInfoBox();
      LoadedItems.Add(mi);
    }

    // TODO use GetUnfilteredItems
    Filter.UpdateSizeRanges(LoadedItems);

    return hide
      ? Root.Source.Except(items).ToList()
      : LoadedItems.Where(Filter.Filter).ToList();
  }

  public void SoftLoad(IEnumerable<MediaItemM> items, bool sort, bool filter) {
    IEnumerable<MediaItemM> toLoad = items.ToArray();

    toLoad = filter
      ? toLoad.Where(Filter.Filter)
      : toLoad;

    toLoad = sort
      ? GetSorted(toLoad)
      : toLoad;

    Reload(toLoad.ToList(), GroupMode.ThenByRecursive, null, true);
    _afterLoad();
  }

  public async Task LoadByTag(MediaItemM[] items, CancellationToken token) {
    IsLoading = true;
    Clear();

    items = await _filterByViewer(items, token);
    if (items.Length == 0) {
      IsLoading = false;
      return;
    }

    var toLoad = _addMediaItems(GetSorted(items).ToList());
    Reload(toLoad, GroupMode.ThenByRecursive, null, true);
    _afterLoad();
    IsLoading = false;

    if (Core.VM.MediaViewer.IsVisible && Root.Source.Count > 0)
      OpenItem(Root.Source[0]);
  }

  private async Task<MediaItemM[]> _filterByViewer(MediaItemM[] items, CancellationToken token) {
    HashSet<Folder.FolderM> foldersSet;

    try {
      foldersSet = await Task.Run(() => items
        .Select(x => x.Folder)
        .Distinct()
        .Where(Core.S.Viewer.CanViewerSeeContentOf)
        .ToHashSet(), token);
    }
    catch (OperationCanceledException) {
      IsLoading = false;
      return [];
    }

    var skip = items.Where(x => !foldersSet.Contains(x.Folder));

    return items.Except(skip).ToArray();
  }

  private void _afterLoad() {
    foreach (var mi in Selected.Items.Where(x => !Root.Source.Contains(x)).ToArray())
      Selected.Set(mi, false);

    OnPropertyChanged(nameof(PositionSlashCount));
    FilteredChangedEventHandler(this, EventArgs.Empty);

    if (Core.VM.MediaItem.Current is { } current && Root.Source.Contains(current))
      ScrollTo(Root, current);
    else {
      Core.VM.MediaItem.Current = null;
      if (Selected.Items.Count != 0)
        ScrollTo(Root, Selected.Items[0]);
    }
  }

  public void Add(MediaItemM mi) {
    Insert(mi);
    OnPropertyChanged(nameof(PositionSlashCount));
    FilteredChangedEventHandler(this, EventArgs.Empty);
  }
}