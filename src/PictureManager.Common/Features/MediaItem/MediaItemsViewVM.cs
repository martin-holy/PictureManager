using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.MediaItem.Image;
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
  
  public event EventHandler SelectionChangedEvent = delegate { };
  public event EventHandler FilteredChangedEvent = delegate { };

  public MediaItemsFilterVM Filter { get; } = new();
  public MediaItemsImport Import { get; } = new();
  public ImageComparerVM? ImageComparer { get; private set; }
  public DragDropHelper.CanDragFunc CanDragFunc { get; }
  public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
  public bool ShowThumbInfo { get => _showThumbInfo; set { _showThumbInfo = value; OnPropertyChanged(); } }

  public RelayCommand SelectAllCommand { get; }

  public MediaItemsViewVM(double thumbScale) : base(thumbScale) {
    CanDragFunc = _canDrag;
    SetFilter(Filter);
    SelectAllCommand = new(() => Selected.Set(Root.Source));
    Selected.ItemsChangedEvent += delegate { _selectionChanged(); };
    Selected.AllDeselectedEvent += delegate { _selectionChanged(); };
    PropertyChanged += _onPropertyChanged;
    FilterAppliedEvent += _onFilterApplied;
  }

  private void _onPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(ThumbScale)))
      ShowThumbInfo = ThumbScale > 0.4;
  }

  protected override void _onIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem, Core.Settings.MediaItem.ScrollExactlyToMediaItem);
  }

  protected override void _onItemOpened(MediaItemM item) {
    Selected.DeselectAll();

    if (LastSelectedRow?.Parent is not CollectionViewGroup<MediaItemM> group) return;
    Core.VM.MainWindow.IsInViewMode = true;
    Core.VM.MediaViewer.SetMediaItems(group.Source.ToList(), item);
  }

  protected override void _onItemSelected(SelectionEventArgs<MediaItemM> e) {
    base._onItemSelected(e);
    Core.VM.MediaItem.Current = Selected.Items.Contains(e.Item) ? e.Item : null;
  }

  private object? _canDrag(object source) {
    if (source is not MediaItemM) return null;
    var data = Selected.Items.OfType<RealMediaItemM>().Select(p => p.FilePath).ToArray();
    return data.Length == 0 ? null : data;
  }

  private void _selectionChanged() {
    // TODO move this condition up
    if (!ReferenceEquals(this, Core.VM.MediaItem.Views.Current) || Core.VM.MediaViewer.IsVisible) return;

    SelectionChangedEvent(this, EventArgs.Empty);
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
    LastSelectedRow = LastSelectedRow?.Parent?.Items
      .OfType<CollectionViewRow<MediaItemM>>()
      .FirstOrDefault(x => x.Leaves.Contains(mi));
    LastSelectedItem = mi;
    OnPropertyChanged(nameof(PositionSlashCount));
    _selectionChanged();
  }

  public async Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
    IsLoading = true;

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

    await Import.Import(newItems);
    var notImported = newItems.Where(x => !x.Success).Select(x => x.MediaItem);
    var items = toProcess.Except(notImported).Where(Core.S.Viewer.CanViewerSee).ToArray();
    _load(items, and, hide);
    IsLoading = false;
  }

  public async Task LoadByTag(MediaItemM[] items, bool and, CancellationToken token) {
    IsLoading = true;
    items = await _filterByViewer(items, token);
    _load(items, and, false);
    IsLoading = false;

    if (Core.VM.MediaViewer.IsVisible && Root.Source.Count > 0)
      OpenItem(Root.Source[0]);
  }

  private void _load(MediaItemM[] items, bool and, bool hide) {
    ImageComparer = null;
    OnPropertyChanged(nameof(ImageComparer));

    if (items.Length == 0) return;

    Core.VM.MediaItem.Current = null;
    Selected.DeselectAll();
    foreach (var mi in items.Where(mi => mi.IsSelected))
      mi.IsSelected = false;

    if (hide) {
      Remove(items);
    }
    else {
      foreach (var mi in items) {
        mi.SetThumbSize();
        mi.SetInfoBox();
      }

      if (and)
        Insert(items);
      else
        Reload(GetSorted(items).ToList(), GroupMode.ThenByRecursive, null, true);
    }

    Filter.UpdateSizeRanges(GetUnfilteredItems().ToArray());
    _selectionChanged();
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
      return [];
    }

    var skip = items.Where(x => !foldersSet.Contains(x.Folder) && Core.S.Viewer.CanViewerSee(x));

    return items.Except(skip).ToArray();
  }

  private void _onFilterApplied(object? sender, EventArgs e) {
    foreach (var mi in Selected.Items.Where(x => !Root.Source.Contains(x)).ToArray())
      Selected.Set(mi, false);

    _selectionChanged();
  }

  public void Add(MediaItemM mi) {
    Insert(mi);
    FilteredChangedEvent(this, EventArgs.Empty);
  }

  public void OnMediaItemRenamed(MediaItemM item) {
    Remove(item);
    Insert(item);
  }

  public async Task CompareImages(Func<ImageComparerVM, Task<List<MediaItemM>>> method) {
    if (ImageComparer == null) {
      ImageComparer = new(Root.Source.ToArray());
      OnPropertyChanged(nameof(ImageComparer));
    }

    Selected.DeselectAll();
    Reload(await method(ImageComparer), GroupMode.ThenByRecursive, null, true);
  }
}