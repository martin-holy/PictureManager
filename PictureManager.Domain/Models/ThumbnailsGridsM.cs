using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridsM : ObservableObject {
    private readonly Core _core;
    private List<ThumbnailsGridM> _all { get; } = new();
    private ThumbnailsGridM _current;
    
    public ThumbnailsGridM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public static double DefaultThumbScale { get; set; } = 1.0;

    public RelayCommand<string> AddThumbnailsGridCommand { get; }
    public RelayCommand<object> CopyPathsCommand { get; }
    public RelayCommand<object> LoadByTagCommand { get; }
    public RelayCommand<object> ShuffleCommand { get; }

    public ThumbnailsGridsM(Core core) {
      _core = core;

      AddThumbnailsGridCommand = new(AddThumbnailsGrid);

      CopyPathsCommand = new(
        () => Clipboard.SetText(string.Join("\n", Current.Selected.Items.Select(x => x.FilePath))),
        () => Current?.Selected.Items.Any() == true);

      LoadByTagCommand = new(async item => await LoadByTag(item));

      ShuffleCommand = new(
        () => Current.Shuffle(),
        () => Current?.FilteredItems.Count > 0);
    }

    public void RemoveMediaItems(List<MediaItemM> items) {
      foreach (var grid in _all)
        grid.Remove(items, Current == grid);
    }

    public void CloseGrid(ThumbnailsGridM grid) {
      grid.Clear();
      grid.SelectionChangedEventHandler -= OnGridSelectionChanged;
      grid.FilteredChangedEventHandler -= OnGridFilteredChanged;
      _all.Remove(grid);

      if (grid.Equals(Current)) {
        Current = null;
        _core.MediaItemsM.Current = null;
      }
    }

    public void SetCurrentGrid(ThumbnailsGridM grid) {
      Current = grid;
      if (Current == null) return;
      Current.UpdateSelected();

      if (Current.NeedReload)
        Current.SoftLoad(Current.LoadedItems, true, true);
    }

    public void AddThumbnailsGridIfNotActive(string tabTitle) {
      if (_core.MainTabsM.Selected?.Content is ThumbnailsGridM grid) {
        if (tabTitle != null)
          grid.MainTabsItem.ContentHeader = tabTitle;

        return;
      }

      AddThumbnailsGrid(tabTitle);
    }

    public void AddThumbnailsGrid(string tabTitle) {
      var grid = new ThumbnailsGridM(_core, DefaultThumbScale, tabTitle);
      _all.Add(grid);
      Current = grid;
      grid.SelectionChangedEventHandler += OnGridSelectionChanged;
      grid.FilteredChangedEventHandler += OnGridFilteredChanged;
      _core.MainTabsM.AddItem(grid.MainTabsItem);
    }

    private void OnGridSelectionChanged(object o, EventArgs e) {
      _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      _core.StatusPanelM.Update();
    }

    private void OnGridFilteredChanged(object o, EventArgs e) {
      _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
    }

    public void ReloadGridsIfContains(MediaItemM[] mediaItems) {
      foreach (var grid in _all) {
        if (!grid.FilteredItems.Any(mediaItems.Contains)) continue;
        if (grid.Equals(Current))
          grid.SoftLoad(grid.LoadedItems, true, true);
        else
          grid.NeedReload = true;
      }
    }

    public async Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
      if (item is FolderM { IsAccessible: false }) return;

      item.IsSelected = true;
      AddThumbnailsGridIfNotActive(and || hide ? null : item.Name);
      await Current.LoadByFolder(item, and, hide, recursive);
    }

    public async Task LoadByTag(object item) {
      var and = Keyboard.IsCtrlOn();
      var hide = Keyboard.IsAltOn();
      var recursive = Keyboard.IsShiftOn();
      var items = item switch {
        RatingTreeM rating => _core.MediaItemsM.DataAdapter.All.Where(x => x.Rating == rating.Rating.Value),
        PersonM person => _core.MediaItemsM.GetMediaItems(person),
        KeywordM keyword => _core.MediaItemsM.GetMediaItems(keyword, recursive),
        GeoNameM geoName => _core.MediaItemsM.GetMediaItems(geoName, recursive),
        _ => Array.Empty<MediaItemM>()
      };

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
        AddThumbnailsGridIfNotActive(tabTitle);
      else
        AddThumbnailsGrid(tabTitle);

      await Current.LoadByTag(items.ToArray());
    }
  }
}