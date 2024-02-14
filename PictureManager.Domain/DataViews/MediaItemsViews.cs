using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.DataViews;

public sealed class MediaItemsViews : ObservableObject {
  private readonly List<MediaItemsView> _all = [];
  private MediaItemsView _current;
    
  public MediaItemsView Current { get => _current; set { _current = value; OnPropertyChanged(); } }
  public static double DefaultThumbScale { get; set; } = 1.0;

  public static RelayCommand<object> FilterSetAndCommand { get; set; }
  public static RelayCommand<object> FilterSetOrCommand { get; set; }
  public static RelayCommand<object> FilterSetNotCommand { get; set; }
  public RelayCommand AddViewCommand { get; }
  public RelayCommand CopyPathsCommand { get; }
  public static RelayCommand<object> LoadByTagCommand { get; set; }
  public RelayCommand ShuffleCommand { get; }
  public RelayCommand<FolderM> RebuildThumbnailsCommand { get; }
  public RelayCommand ViewModifiedCommand { get; }

  public MediaItemsViews() {
    FilterSetAndCommand = new(item => Current.Filter.Set(item, DisplayFilter.And), _ => Current != null, Res.IconFilter, "Filter And");
    FilterSetOrCommand = new(item => Current.Filter.Set(item, DisplayFilter.Or), _ => Current != null, Res.IconFilter, "Filter Or");
    FilterSetNotCommand = new(item => Current.Filter.Set(item, DisplayFilter.Not), _ => Current != null, Res.IconFilter, "Filter Not");

    AddViewCommand = new(() => AddView(string.Empty), Res.IconPlus, "Add Media Items View Tab");
    CopyPathsCommand = new(
      () => Clipboard.SetText(string.Join("\n", Current.Selected.Items.Select(x => x.FilePath))),
      () => Current?.Selected.Items.Any() == true, null, "Copy Paths");
    LoadByTagCommand = new(LoadByTag, null, "Load");
    ShuffleCommand = new(
      () => Current.Shuffle(),
      () => Current?.FilteredItems.Count > 0, null, "Shuffle");
    RebuildThumbnailsCommand = new(
      x => RebuildThumbnails(x, Keyboard.IsShiftOn()),
      x => x != null || Current?.FilteredItems.Count > 0, null, "Rebuild Thumbnails");
    ViewModifiedCommand = new(ViewModified, Res.IconImageMultiple);
  }

  public void RemoveMediaItems(IList<MediaItemM> items) {
    foreach (var view in _all)
      view.Remove(items, Current == view);
  }

  public void CloseView(MediaItemsView view) {
    view.Clear();
    view.SelectionChangedEventHandler -= OnViewSelectionChanged;
    view.FilteredChangedEventHandler -= OnViewFilteredChanged;
    _all.Remove(view);
    if (!ReferenceEquals(view, Current)) return;
    Current = null;
    Core.MediaItemsM.Current = null;
  }

  public void SetCurrentView(MediaItemsView view) {
    Current = view;
    Current?.UpdateSelected();
    Core.MediaItemsM.Current = Current?.Selected.Items.Count > 0
      ? Current.Selected.Items[0]
      : null;
  }

  private void AddViewIfNotActive(string tabName) {
    if (Core.MainTabs.Selected?.Data is MediaItemsView) {
      if (tabName != null)
        Core.MainTabs.Selected.Name = tabName;

      return;
    }

    AddView(tabName);
  }

  private void AddView(string tabName) {
    var view = new MediaItemsView(DefaultThumbScale);
    _all.Add(view);
    Current = view;
    view.SelectionChangedEventHandler += OnViewSelectionChanged;
    view.FilteredChangedEventHandler += OnViewFilteredChanged;
    Core.MainTabs.Add(Res.IconImageMultiple, tabName, view);
  }

  private void OnViewSelectionChanged(object o, EventArgs e) {
    Core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
    _ = Core.MediaItemsStatusBarM.UpdateFileSize();
  }

  private void OnViewFilteredChanged(object o, EventArgs e) {
    Core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
  }

  public void ReWrapViews(MediaItemM[] mediaItems) =>
    _all.ForEach(x => x.ReWrapAll(mediaItems));

  public void UpdateViews(MediaItemM[] mediaItems) =>
    _all.ForEach(x => x.Update(mediaItems));

  public Task LoadByFolder(ITreeItem item) {
    if (item is FolderM { IsAccessible: false }) return Task.CompletedTask;
    var and = Keyboard.IsCtrlOn();
    var hide = Keyboard.IsAltOn();
    var recursive = Keyboard.IsShiftOn();

    item.IsSelected = true;
    AddViewIfNotActive(and || hide ? null : item.Name);
    return Current.LoadByFolder(item, and, hide, recursive);
  }

  public async void LoadByTag(object item) {
    var and = Keyboard.IsCtrlOn();
    var hide = Keyboard.IsAltOn();
    var items = Core.Db.MediaItems.GetItems(item, Keyboard.IsShiftOn());

    // if CTRL is pressed, add new items to already loaded items
    if (and) items = Current.LoadedItems.Union(items);
    // if ALT is pressed, remove new items from already loaded items
    if (hide) items = Current.LoadedItems.Except(items);

    var tabTitle = and || hide
      ? null
      : item switch {
        RatingTreeM rating => rating.Rating.Value.ToString(),
        PersonM person => person.Name,
        KeywordM keyword => keyword.Name,
        GeoNameM geoName => geoName.Name,
        _ => string.Empty
      };

    if (and || hide)
      AddViewIfNotActive(null);
    else
      AddView(tabTitle);

    await Current.LoadByTag(items.ToArray());
  }

  public void SelectAndScrollToCurrentMediaItem() {
    if (Current != null)
      Current.SelectAndScrollToCurrentMediaItem();
    else
      Core.MediaItemsM.Current = null;
  }

  private void RebuildThumbnails(FolderM folder, bool recursive) {
    var mediaItems = (folder == null
        ? Current?.GetSelectedOrAll()?.OfType<RealMediaItemM>()
        : folder.GetMediaItems(recursive))?
      .Cast<MediaItemM>().ToArray();

    if (mediaItems == null) return;

    foreach (var mi in mediaItems) {
      File.Delete(mi.FilePathCache);
      mi.OnPropertyChanged(nameof(mi.FilePathCache));
    }
  }

  private async void ViewModified() {
    AddView("Modified");
    await Current.LoadByTag(Core.Db.MediaItems.GetModified().ToArray());
  }
}