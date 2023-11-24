using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.DataViews {
  public class MediaItemsView : CollectionViewMediaItems {
    private bool _isLoading;
    private bool _isImporting;
    private int _importCount;
    private int _importDoneCount;
    private readonly IProgress<int> _importProgress;
    private readonly WorkTask _importTask = new();

    public event EventHandler SelectionChangedEventHandler = delegate { };
    public event EventHandler FilteredChangedEventHandler = delegate { };

    public List<MediaItemM> LoadedItems { get; } = new();
    public List<MediaItemM> FilteredItems { get; } = new();
    public MediaItemsFilterM Filter { get; } = new();
    public DragDropHelper.CanDragFunc CanDragFunc { get; }
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
    public bool IsImporting { get => _isImporting; set { _isImporting = value; OnPropertyChanged(); } }
    public int ImportCount { get => _importCount; set { _importCount = value; OnPropertyChanged(); } }
    public int ImportDoneCount { get => _importDoneCount; set { _importDoneCount = value; OnPropertyChanged(); } }
    public string PositionSlashCount =>
      Selected.Items.Count == 0
        ? FilteredItems.Count.ToString()
        : $"{FilteredItems.IndexOf(Selected.Items[0]) + 1}/{FilteredItems.Count}";

    public RelayCommand<object> CancelImportCommand { get; }
    public RelayCommand<object> SelectAllCommand { get; }

    public MediaItemsView(double thumbScale) : base(thumbScale) {
      CanDragFunc = CanDrag;
      _importProgress = new Progress<int>(x => ImportDoneCount += x);

      CancelImportCommand = new(CancelImport);
      SelectAllCommand = new(() => Selected.Set(FilteredItems));

      Filter.FilterChangedEventHandler += delegate { SoftLoad(LoadedItems, true, true); };
      Selected.ItemsChangedEventHandler += delegate { SelectionChanged(); };
      Selected.AllDeselectedEventHandler += delegate { SelectionChanged(); };
    }

    public override void OnItemOpened(MediaItemM item) {
      if (item == null) return;

      Selected.DeselectAll();
      Core.MainWindowM.IsInViewMode = true;
      // TODO open group or all with default sort or all sorted by groups or ...?
      Core.MediaViewerM.SetMediaItems(FilteredItems.ToList(), item);
    }

    public override void OnItemSelected(SelectionEventArgs<MediaItemM> e) {
      base.OnItemSelected(e);
      Core.MediaItemsM.Current = Selected.Items.Contains(e.Item) ? e.Item : null;
    }

    private object CanDrag(object source) {
      if (source is not MediaItemM) return null;
      var data = Selected.Items.Select(p => p.FilePath).ToArray();
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
      if (!ReferenceEquals(this, Core.MediaItemsViews.Current) || Core.MediaViewerM.IsVisible) return;

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

      if (needReload) {
        TopItem = ListExtensions.GetNextOrPreviousItem(Root.Source, items);
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

    // TODO sort
    public static IEnumerable<MediaItemM> Sort(IEnumerable<MediaItemM> items) =>
      items.OrderBy(x => x.FileName);

    private MediaItemM GetItemToScrollTo() =>
      FilteredItems.Contains(Core.MediaItemsM.Current)
        ? Core.MediaItemsM.Current
        : Selected.Items.FirstOrDefault();

    public void SelectAndScrollToCurrentMediaItem() {
      var mi = GetItemToScrollTo();
      Selected.DeselectAll();
      Core.MediaItemsM.Current = mi;
      if (mi == null) return;
      Selected.Set(mi, true);
      ScrollTo(Root, mi);
      SelectionChanged();
    }

    public async Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
      IsLoading = true;
      if (!and && !hide)
        Clear();

      var folders = Core.FoldersM.GetFolders(item, recursive);
      var newItems = new List<MediaItemMetadata>();
      var toLoad = new List<MediaItemM>();

      foreach (var folder in folders) {
        if (!Directory.Exists(folder.FullPath)
          || !Core.ViewersM.CanViewerSeeContentOf(folder)) continue;

        // add MediaItems from current Folder to dictionary for faster search
        var mediaItems = folder.MediaItems.ToDictionary(x => x.FileName);

        // get new items from folder
        newItems.AddRange(from file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)
          where MediaItemsM.IsSupportedFileType(file)
          select Path.GetFileName(file)
          into fileName
          where !mediaItems.Remove(fileName)
          select new MediaItemMetadata(Core.Db.MediaItems.ItemCreate(folder, fileName)));

        // remove MediaItems deleted outside of this application
        Core.MediaItemsM.DataAdapter.ItemsDelete(mediaItems.Values.ToArray());

        toLoad.AddRange(folder.MediaItems);
      }

      await ReadMetadata(newItems);
      var notImported = newItems.Where(x => !x.Success).Select(x => x.MediaItem);
      AddMediaItems(Sort(toLoad.Except(notImported)).ToList(), and, hide);
      Reload(FilteredItems.ToList(), GroupMode.ThenByRecursive, null, true);
      AfterLoad();
      IsLoading = false;
    }

    private async void CancelImport() =>
      await _importTask.Cancel();

    private async Task ReadMetadata(List<MediaItemMetadata> items) {
      if (items.Count == 0) return;
      IsLoading = false;
      IsImporting = true;
      ImportCount = items.Count;
      ImportDoneCount = 0;

      await _importTask.Start(new(() => {
        try {
          Parallel.ForEach(
            items.Where(x => x.MediaItem.MediaType == MediaType.Image),
            new() { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _importTask.Token },
            mim => {
              Core.MediaItemsM.ReadMetadata(mim, false);
              _importProgress.Report(1);
            });
        }
        catch (OperationCanceledException) { }
      }));

      foreach (var mim in items.Where(x => x.MediaItem.MediaType == MediaType.Video)) {
        Core.MediaItemsM.ReadMetadata(mim, false);
        _importProgress.Report(1);
      }

      foreach (var mim in items)
        if (mim.Success)
          mim.FindRefs();
        else
          Core.MediaItemsM.DataAdapter.ItemDelete(mim.MediaItem);

      IsImporting = false;
      IsLoading = true;
    }

    private void AddMediaItems(List<MediaItemM> items, bool and = false, bool hide = false) {
      foreach (var mi in items) {
        if (hide) {
          LoadedItems.Remove(mi);
          FilteredItems.Remove(mi);
          if (Selected.Items.Remove(mi))
            mi.IsSelected = false;

          continue;
        }

        if (and && LoadedItems.Contains(mi)) continue;
        if (!Core.ViewersM.CanViewerSee(mi)) continue;

        mi.SetThumbSize();
        mi.SetInfoBox();
        LoadedItems.Add(mi);
      }

      Filter.UpdateSizeRanges(LoadedItems, true);
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
        ? Sort(toLoad)
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
        .Where(x => Core.ViewersM.CanViewerSeeContentOf(x))
        .ToHashSet());

      var skip = items
        .Where(x => !foldersSet.Contains(x.Folder));

      AddMediaItems(Sort(items.Except(skip)).ToList());
      Reload(FilteredItems.ToList(), GroupMode.ThenByRecursive, null, true);
      AfterLoad();
      IsLoading = false;

      if (Core.MediaViewerM.IsVisible && FilteredItems.Count > 0)
        OpenItem(FilteredItems[0]);
    }

    private void AfterLoad(bool maxSizeSelection = false) {
      foreach (var mi in Selected.Items.Where(x => !FilteredItems.Contains(x)).ToArray())
        Selected.Set(mi, false);

      OnPropertyChanged(nameof(PositionSlashCount));
      FilteredChangedEventHandler(this, EventArgs.Empty);
      //Filter.UpdateSizeRanges(LoadedItems, maxSizeSelection);

      if (FilteredItems.Contains(Core.MediaItemsM.Current))
        ScrollTo(Root, Core.MediaItemsM.Current);
      else {
        Core.MediaItemsM.Current = null;
        if (Selected.Items.Count != 0)
          ScrollTo(Root, Selected.Items[0]);
      }
    }
  }
}
