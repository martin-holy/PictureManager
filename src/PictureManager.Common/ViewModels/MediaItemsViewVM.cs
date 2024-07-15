using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.HelperClasses;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.ViewModels;

public class MediaItemsViewVM : CollectionViewMediaItems {
  private bool _isLoading;
  
  public event EventHandler SelectionChangedEventHandler = delegate { };
  public event EventHandler FilteredChangedEventHandler = delegate { };

  public List<MediaItemM> LoadedItems { get; } = [];
  public List<MediaItemM> FilteredItems { get; } = [];
  public MediaItemsFilterVM Filter { get; } = new();
  public MediaItemsImport Import { get; } = new();
  public DragDropHelper.CanDragFunc CanDragFunc { get; }
  public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
  
  public string PositionSlashCount =>
    Selected.Items.Count == 0
      ? FilteredItems.Count.ToString()
      : $"{FilteredItems.IndexOf(Selected.Items[0]) + 1}/{FilteredItems.Count}";

  public RelayCommand SelectAllCommand { get; }

  public MediaItemsViewVM(double thumbScale) : base(thumbScale) {
    CanDragFunc = CanDrag;
    SelectAllCommand = new(() => Selected.Set(FilteredItems));
    Filter.FilterChangedEventHandler += delegate { SoftLoad(LoadedItems, true, true); };
    Selected.ItemsChangedEvent += delegate { SelectionChanged(); };
    Selected.AllDeselectedEvent += delegate { SelectionChanged(); };
  }

  public override void OnIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem, Core.Settings.MediaItem.ScrollExactlyToMediaItem);
  }

  public override void OnItemOpened(MediaItemM item) {
    Selected.DeselectAll();
    Core.VM.MainWindow.IsInViewMode = true;
    // TODO open group or all with default sort or all sorted by groups or ...?
    Core.VM.MediaViewer.SetMediaItems(FilteredItems.ToList(), item);
  }

  public override void OnItemSelected(SelectionEventArgs<MediaItemM> e) {
    base.OnItemSelected(e);
    Core.VM.MediaItem.Current = Selected.Items.Contains(e.Item) ? e.Item : null;
  }

  private object? CanDrag(object source) {
    if (source is not MediaItemM) return null;
    var data = Selected.Items.OfType<RealMediaItemM>().Select(p => p.FilePath).ToArray();
    return data.Length == 0 ? null : data;
  }

  public void Clear() {
    foreach (var item in Selected.Items)
      item.IsSelected = false;

    Selected.Items.Clear();
    LoadedItems.Clear();
    FilteredItems.Clear();
  }

  private void SelectionChanged() {
    if (!ReferenceEquals(this, Core.VM.MediaItem.Views.Current) || Core.VM.MediaViewer.IsVisible) return;

    SelectionChangedEventHandler(this, EventArgs.Empty);
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  public void UpdateSelected() {
    foreach (var mi in Selected.Items)
      mi.IsSelected = true;

    foreach (var mi in FilteredItems.Except(Selected.Items).Where(x => x.IsSelected))
      mi.IsSelected = false;

    SelectionChanged();
  }

  public void Remove(IList<MediaItemM> items, bool isCurrent) {
    var needReload = false;

    foreach (var item in items) {
      LoadedItems.Remove(item);

      if (FilteredItems.Remove(item))
        needReload = true;

      if (isCurrent)
        Selected.Set(item, false);
      else
        Selected.Items.Remove(item);
    }

    if (needReload && Root != null) {
      TopItem = Root.Source.GetNextOrPreviousItem(items);
      Remove(items.ToArray());
    }

    SelectionChanged();
  }

  public List<MediaItemM> GetSelectedOrAll() =>
    Selected.Items.Count == 0 ? FilteredItems : Selected.Items.ToList();

  public void Shuffle() {
    LoadedItems.Shuffle();
    SoftLoad(LoadedItems, false, true);
  }

  public void Sort() =>
    SoftLoad(LoadedItems, true, true);

  public static IEnumerable<MediaItemM> GetSorted(IEnumerable<MediaItemM> items) =>
    items.OrderBy(x => x.FileName);

  private MediaItemM? GetItemToScrollTo() =>
    Core.VM.MediaItem.Current is { } current && FilteredItems.Contains(current)
      ? current
      : Selected.Items.FirstOrDefault();

  public void SelectAndScrollToCurrentMediaItem() {
    var mi = GetItemToScrollTo();
    Core.VM.MediaItem.Current = mi;
    if (mi == null) return;
    Selected.Set(mi, true);
    ScrollTo(Root, mi, Core.Settings.MediaItem.ScrollExactlyToMediaItem);
    SelectionChanged();
  }

  public Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
    IsLoading = true;
    if (!and && !hide)
      Clear();

    var folders = Core.S.Folder.GetFolders(item, recursive);
    var newItems = new List<MediaItemMetadata>();
    var toLoad = new List<MediaItemM>();

    foreach (var folder in folders) {
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

      toLoad.AddRange(folder.MediaItems);
    }

    return Import.Import(newItems).ContinueWith(delegate {
      var notImported = newItems.Where(x => !x.Success).Select(x => x.MediaItem);
      AddMediaItems(GetSorted(toLoad.Except(notImported)).ToList(), and, hide);
      Reload(FilteredItems.ToList(), GroupMode.ThenByRecursive, null, true);
      if (newItems.Count > 0 && Root != null) CollectionViewGroup<MediaItemM>.ReWrapAll(Root);
      AfterLoad();
      IsLoading = false;
    }, Tasks.UiTaskScheduler);
  }

  private void AddMediaItems(List<MediaItemM> items, bool and = false, bool hide = false) {
    Selected.DeselectAll();
    foreach (var mi in items) {
      if (mi.IsSelected) mi.IsSelected = false;

      if (hide) {
        LoadedItems.Remove(mi);
        FilteredItems.Remove(mi);
        continue;
      }

      if (and && LoadedItems.Contains(mi)) continue;
      if (!Core.S.Viewer.CanViewerSee(mi)) continue;

      mi.SetThumbSize();
      mi.SetInfoBox();
      LoadedItems.Add(mi);
    }

    Filter.UpdateSizeRanges(LoadedItems);
    if (hide) return;
    FilteredItems.Clear();
    FilteredItems.AddRange(LoadedItems.Where(Filter.Filter));
  }

  public void SoftLoad(IEnumerable<MediaItemM> items, bool sort, bool filter) {
    IEnumerable<MediaItemM> toLoad = items.ToArray();
    FilteredItems.Clear();

    toLoad = filter
      ? toLoad.Where(Filter.Filter)
      : toLoad;

    toLoad = sort
      ? GetSorted(toLoad)
      : toLoad;

    FilteredItems.AddRange(toLoad);
    Reload(FilteredItems.ToList(), GroupMode.ThenByRecursive, null, true);
    AfterLoad();
  }

  public async Task LoadByTag(MediaItemM[] items) {
    IsLoading = true;
    Clear();

    var foldersSet = await Task.Run(() => items
      .Select(x => x.Folder)
      .Distinct()
      .Where(x => Core.S.Viewer.CanViewerSeeContentOf(x))
      .ToHashSet());

    var skip = items
      .Where(x => !foldersSet.Contains(x.Folder));

    AddMediaItems(GetSorted(items.Except(skip)).ToList());
    Reload(FilteredItems.ToList(), GroupMode.ThenByRecursive, null, true);
    AfterLoad();
    IsLoading = false;

    if (Core.VM.MediaViewer.IsVisible && FilteredItems.Count > 0)
      OpenItem(FilteredItems[0]);
  }

  private void AfterLoad() {
    foreach (var mi in Selected.Items.Where(x => !FilteredItems.Contains(x)).ToArray())
      Selected.Set(mi, false);

    OnPropertyChanged(nameof(PositionSlashCount));
    FilteredChangedEventHandler(this, EventArgs.Empty);

    if (Core.VM.MediaItem.Current is { } current && FilteredItems.Contains(current))
      ScrollTo(Root, current);
    else {
      Core.VM.MediaItem.Current = null;
      if (Selected.Items.Count != 0)
        ScrollTo(Root, Selected.Items[0]);
    }
  }
}