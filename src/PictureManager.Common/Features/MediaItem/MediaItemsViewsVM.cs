using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Features.Segment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemsViewsVM : ObservableObject {
  private readonly List<MediaItemsViewVM> _all = [];
  private MediaItemsViewVM? _current;

  public MediaItemsViewVM? Current { get => _current; set { _current = value; OnPropertyChanged(); } }

  public static RelayCommand<object> FilterSetAndCommand { get; set; } = null!;
  public static RelayCommand<object> FilterSetOrCommand { get; set; } = null!;
  public static RelayCommand<object> FilterSetNotCommand { get; set; } = null!;
  public static RelayCommand AddViewCommand { get; set; } = null!;
  public static RelayCommand CopyPathsCommand { get; set; } = null!;
  public static AsyncRelayCommand<object> LoadByTagCommand { get; set; } = null!;
  public static RelayCommand ShuffleCommand { get; set; } = null!;
  public static RelayCommand SortCommand { get; set; } = null!;
  public static RelayCommand<FolderM> RebuildThumbnailsCommand { get; set; } = null!;
  public static AsyncRelayCommand ViewModifiedCommand { get; set; } = null!;

  public MediaItemsViewsVM() {
    FilterSetAndCommand = new(item => Current!.Filter.Set(item, DisplayFilter.And), _ => Current != null, Res.IconFilter, "Filter And");
    FilterSetOrCommand = new(item => Current!.Filter.Set(item, DisplayFilter.Or), _ => Current != null, Res.IconFilter, "Filter Or");
    FilterSetNotCommand = new(item => Current!.Filter.Set(item, DisplayFilter.Not), _ => Current != null, Res.IconFilter, "Filter Not");

    AddViewCommand = new(() => AddView(string.Empty), Res.IconPlus, "Add Media Items View Tab");
    CopyPathsCommand = new(
      () => Clipboard.SetText(string.Join("\n", Current!.Selected.Items.Select(x => x.FilePath))),
      () => Current?.Selected.Items.Any() == true, null, "Copy Paths");
    LoadByTagCommand = new(LoadByTag, null, "Load");
    ShuffleCommand = new(
      () => Current!.Shuffle(),
      () => Current?.FilteredItems.Count > 0, MH.UI.Res.IconRandom, "Shuffle");
    SortCommand = new(
      () => Current!.Sort(),
      () => Current?.FilteredItems.Count > 0, MH.UI.Res.IconSort, "Sort");
    RebuildThumbnailsCommand = new(
      x => RebuildThumbnails(x, Keyboard.IsShiftOn()),
      x => x != null || Current?.FilteredItems.Count > 0, null, "Rebuild Thumbnails");
    ViewModifiedCommand = new(ViewModified, Res.IconImageMultiple, "Show modified");
  }

  public void RemoveMediaItems(IList<MediaItemM> items) {
    foreach (var view in _all)
      view.Remove(items, Current == view);
  }

  public void CloseView(MediaItemsViewVM view) {
    view.Clear();
    view.SelectionChangedEventHandler -= OnViewSelectionChanged;
    view.FilteredChangedEventHandler -= OnViewFilteredChanged;
    _all.Remove(view);
    if (!ReferenceEquals(view, Current)) return;
    Current = null;
    Core.VM.MediaItem.Current = null;
  }

  public void SetCurrentView(MediaItemsViewVM? view) {
    Current = view;
    Current?.UpdateSelected();
  }

  private MediaItemsViewVM AddViewIfNotActive(string? tabName) {
    if (Core.VM.MainTabs.Selected?.Data is not MediaItemsViewVM view)
      return AddView(tabName ?? string.Empty);
    
    if (tabName != null)
      Core.VM.MainTabs.Selected.Name = tabName;

    return view;
  }

  private MediaItemsViewVM AddView(string tabName) {
    var view = new MediaItemsViewVM(Core.Settings.MediaItem.MediaItemThumbScale);
    _all.Add(view);
    Current = view;
    view.SelectionChangedEventHandler += OnViewSelectionChanged;
    view.FilteredChangedEventHandler += OnViewFilteredChanged;
    Core.VM.MainTabs.Add(Res.IconImageMultiple, tabName, view);
    return view;
  }

  private void OnViewSelectionChanged(object? o, EventArgs e) {
    Core.VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    _ = Core.VM.MainWindow.StatusBar.UpdateFileSize();
  }

  private void OnViewFilteredChanged(object? o, EventArgs e) {
    Core.VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
  }

  public void ReWrapViews(MediaItemM[] mediaItems) =>
    _all.ForEach(x => x.ReWrapAll(mediaItems));

  public void UpdateViews(MediaItemM[] mediaItems) =>
    _all.ForEach(x => x.Update(mediaItems));

  public Task LoadByFolder(ITreeItem item) {
    if (Current?.Import.IsImporting == true) return Task.CompletedTask;
    if (item is FolderM { IsAccessible: false }) return Task.CompletedTask;
    var and = Keyboard.IsCtrlOn();
    var hide = Keyboard.IsAltOn();
    var recursive = Keyboard.IsShiftOn();
    var view = AddViewIfNotActive(and || hide ? null : item.Name);
    item.IsSelected = true;
    
    return view.LoadByFolder(item, and, hide, recursive);
  }

  public Task LoadByTag(object? item, CancellationToken token) {
    if (item == null) return Task.CompletedTask;
    var and = Keyboard.IsCtrlOn() && Current != null;
    var items = Core.R.MediaItem.GetItems(item, Keyboard.IsShiftOn()).OfType<RealMediaItemM>().Cast<MediaItemM>();

    if (and) items = Current!.LoadedItems.Union(items);

    var tabTitle = and
      ? null
      : item switch {
        RatingTreeM rating => rating.Rating.Value.ToString(),
        PersonM person => person.Name,
        PersonM[] => "By People",
        SegmentM[] => "By Segments",
        KeywordM keyword => keyword.Name,
        GeoNameM geoName => geoName.Name,
        _ => string.Empty
      };

    var view = and ? AddViewIfNotActive(null) : AddView(tabTitle!);
    return view.LoadByTag(items.ToArray(), token);
  }

  public void SelectAndScrollToCurrentMediaItem() {
    if (Current != null)
      Current.SelectAndScrollToCurrentMediaItem();
    else
      Core.VM.MediaItem.Current = null;
  }

  private void RebuildThumbnails(FolderM? folder, bool recursive) {
    var mediaItems = (folder == null
        ? Current!.GetSelectedOrAll().OfType<RealMediaItemM>()
        : folder.GetMediaItems(recursive))
      .Cast<MediaItemM>().ToArray();

    foreach (var mi in mediaItems) {
      File.Delete(mi.FilePathCache);
      mi.OnPropertyChanged(nameof(mi.FilePathCache));
    }
  }

  private Task ViewModified(CancellationToken token) =>
    AddView("Modified").LoadByTag(Core.R.MediaItem.GetModified().ToArray(), token);

  public Task ViewMediaItems(MediaItemM[] items, string name, CancellationToken token) =>
    AddView(name).LoadByTag(items, token);
}