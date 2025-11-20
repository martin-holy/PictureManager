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

  public MediaItemsViewVM? Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
  public static RelayCommand<object> FilterSetAndCommand { get; set; } = null!;
  public static RelayCommand<object> FilterSetOrCommand { get; set; } = null!;
  public static RelayCommand<object> FilterSetNotCommand { get; set; } = null!;
  public static RelayCommand AddViewCommand { get; set; } = null!;
  public static RelayCommand CopyPathsCommand { get; set; } = null!;
  public static AsyncRelayCommand<object> LoadByTagCommand { get; set; } = null!;
  public static RelayCommand<FolderM> RebuildThumbnailsCommand { get; set; } = null!;
  public static AsyncRelayCommand ViewModifiedCommand { get; set; } = null!;
  public static AsyncRelayCommand CompareAverageHashCommand { get; set; } = null!;
  public static AsyncRelayCommand ComparePHashCommand { get; set; } = null!;

  public event EventHandler? CurrentViewSelectionChangedEvent;

  public MediaItemsViewsVM() {
    FilterSetAndCommand = new(item => Current!.Filter.Set(item, DisplayFilter.And), _ => Current != null, Res.IconFilter, "Filter And");
    FilterSetOrCommand = new(item => Current!.Filter.Set(item, DisplayFilter.Or), _ => Current != null, Res.IconFilter, "Filter Or");
    FilterSetNotCommand = new(item => Current!.Filter.Set(item, DisplayFilter.Not), _ => Current != null, Res.IconFilter, "Filter Not");

    AddViewCommand = new(() => _addView(string.Empty), Res.IconPlus, "Add Media Items View Tab");
    CopyPathsCommand = new(
      () => Clipboard.SetText(string.Join("\n", Current!.Selected.Items.Select(x => x.FilePath))),
      () => Current?.Selected.Items.Any() == true, null, "Copy Paths");
    LoadByTagCommand = new(LoadByTag, null, "Load");
    RebuildThumbnailsCommand = new(
      x => _rebuildThumbnails(x, Keyboard.IsShiftOn()),
      x => x != null || Current?.Root.Source.Count > 0, null, "Rebuild Thumbnails");
    ViewModifiedCommand = new(_viewModified, Res.IconImageMultiple, "Show modified");
    CompareAverageHashCommand = new(_ => _current!.CompareImages(c => c.CompareAverageHash()), () => _current != null, Res.IconCompare, "Compare images using average hash");
    ComparePHashCommand = new(_ => _current!.CompareImages(c => c.ComparePHash()), () => _current != null, Res.IconCompare, "Compare images using perceptual hash");
  }

  public void RemoveMediaItems(IList<MediaItemM> items) {
    foreach (var view in _all)
      view.Remove(items, Current == view);
  }

  public void CloseView(MediaItemsViewVM view) {
    view.Selected.DeselectAll();
    view.SelectionChangedEvent -= _onViewSelectionChanged;
    view.FilteredChangedEvent -= _onViewFilteredChanged;
    _all.Remove(view);
    if (!ReferenceEquals(view, Current)) return;
    Current = null;
    Core.VM.MediaItem.Current = null;
  }

  public void SetCurrentView(MediaItemsViewVM? view) {
    Current = view;
    Current?.UpdateSelected();
  }

  private MediaItemsViewVM _addViewIfNotActive(string? tabName) {
    if (Core.VM.MainTabs.Selected?.Data is not MediaItemsViewVM view)
      return _addView(tabName ?? string.Empty);
    
    if (tabName != null)
      Core.VM.MainTabs.Selected.Name = tabName;

    return view;
  }

  private MediaItemsViewVM _addView(string tabName) {
    var view = new MediaItemsViewVM(Core.Settings.MediaItem.MediaItemThumbScale);
    _all.Add(view);
    Current = view;
    view.SelectionChangedEvent += _onViewSelectionChanged;
    view.FilteredChangedEvent += _onViewFilteredChanged;
    Core.VM.MainTabs.Activate(Res.IconImageMultiple, tabName, view);
    return view;
  }

  private void _onViewSelectionChanged(object? o, EventArgs e) {
    if (!ReferenceEquals(o, Current) || Core.VM.MediaViewer.IsVisible) return;

    CurrentViewSelectionChangedEvent?.Invoke(this, EventArgs.Empty);
    Core.VM.MainWindow.TreeViewCategories.MarkUsedKeywordsAndPeople();
    _ = Core.VM.MainWindow.StatusBar.UpdateFileSize();
  }

  private void _onViewFilteredChanged(object? o, EventArgs e) {
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
    var view = _addViewIfNotActive(and || hide ? null : item.Name);
    item.IsSelected = true;
    
    return view.LoadByFolder(item, and, hide, recursive);
  }

  public Task LoadByTag(object? item, CancellationToken token) {
    if (item == null) return Task.CompletedTask;
    var and = Keyboard.IsCtrlOn() && Current != null;
    var items = Core.R.MediaItem.GetItems(item, Keyboard.IsShiftOn()).OfType<RealMediaItemM>().Cast<MediaItemM>();

    if (and) items = items.Except(Current!.GetUnfilteredItems());

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

    var view = and ? _addViewIfNotActive(null) : _addView(tabTitle!);
    return view.LoadByTag(items.ToArray(), and, token);
  }

  public void SelectAndScrollToCurrentMediaItem() {
    if (Current != null)
      Current.SelectAndScrollToCurrentMediaItem();
    else
      Core.VM.MediaItem.Current = null;
  }

  private void _rebuildThumbnails(FolderM? folder, bool recursive) {
    var mediaItems = (folder == null
        ? Current!.GetSelectedOrAll().OfType<RealMediaItemM>()
        : folder.GetMediaItems(recursive))
      .Cast<MediaItemM>()
      .Where(x => File.Exists(x.FilePathCache))
      .ToArray();

    foreach (var mi in mediaItems) {
      File.Delete(mi.FilePathCache);
      mi.OnPropertyChanged(nameof(mi.FilePathCache));
    }
  }

  private Task _viewModified(CancellationToken token) =>
    _addView("Modified").LoadByTag(Core.R.MediaItem.GetModified().ToArray(), false, token);

  public Task ViewMediaItems(MediaItemM[] items, string name, CancellationToken token) =>
    _addView(name).LoadByTag(items, false, token);
}