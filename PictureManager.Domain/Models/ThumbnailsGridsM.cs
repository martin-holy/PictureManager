using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridsM : ObservableObject {
    private readonly Core _core;
    private ThumbnailsGridM _current;

    public ObservableCollection<ThumbnailsGridM> All { get; } = new();
    public ThumbnailsGridM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public double DefaultThumbScale { get; set; } = 1.0;
    public event EventHandler<ObjectEventArgs<(ThumbnailsGridM, string)>> ThumbnailsGridAddedEventHandler = delegate { };
    public Action<string> AddThumbnailsGridIfNotActive { get; set; }

    public RelayCommand<string> AddThumbnailsGridCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterAndCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterOrCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterNotCommand { get; }
    public RelayCommand<object> ClearFiltersCommand { get; }
    public RelayCommand<object> SelectNotModifiedCommand { get; }
    public RelayCommand<object> ShuffleCommand { get; }
    public RelayCommand<object> ReapplyFilterCommand { get; }

    public ThumbnailsGridsM(Core core) {
      _core = core;

      AddThumbnailsGridCommand = new(AddThumbnailsGrid);
      ActivateFilterAndCommand = new(
        item => _ = Current?.ActivateFilter(item, DisplayFilter.And),
        item => item != null);
      ActivateFilterOrCommand = new(
        item => _ = Current?.ActivateFilter(item, DisplayFilter.Or),
        item => item != null);
      ActivateFilterNotCommand = new(
        item => _ = Current?.ActivateFilter(item, DisplayFilter.Not),
        item => item != null);
      ClearFiltersCommand = new(() => _ = Current?.ClearFilters());
      SelectNotModifiedCommand = new(
        () => Current?.SelectNotModified(_core.MediaItemsM.ModifiedItems),
        () => Current?.FilteredItems.Count > 0);
      ShuffleCommand = new(
        () => {
          Current.Shuffle();
          _ = Current.ThumbsGridReloadItems();
        },
        () => Current?.FilteredItems.Count > 0);
      ReapplyFilterCommand = new(
        async () => await Current.ReapplyFilter(),
        () => Current != null);
    }

    private ThumbnailsGridM AddThumbnailsGrid(MediaItemsM mediaItemsM, TitleProgressBarM progressBar) {
      var grid = new ThumbnailsGridM(mediaItemsM, progressBar, DefaultThumbScale);
      All.Add(grid);
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      return grid;
    }

    public void RemoveMediaItem(MediaItemM item) {
      foreach (var grid in All)
        grid.Remove(item, Current == grid);
    }

    public async Task SetCurrentGrid(ThumbnailsGridM grid) {
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      if (Current == null) return;

      Current.UpdateSelected();
      
      if (Current.NeedReload) {
        await Current.ThumbsGridReloadItems();
        Current.UpdatePositionSlashCount();
      }
    }

    public void AddThumbnailsGrid(string tabTitle) {
      var model = AddThumbnailsGrid(_core.MediaItemsM, _core.TitleProgressBarM);

      model.SelectionChangedEventHandler += (_, _) => {
        _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        _core.StatusPanelM.OnPropertyChanged(nameof(_core.StatusPanelM.FileSize));
      };

      model.FilteredChangedEventHandler += (_, _) => {
        _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      };

      ThumbnailsGridAddedEventHandler(this, new((model, tabTitle)));
    }

    public async Task LoadByTag(ITreeItem item, bool and, bool hide, bool recursive) {
      var items = item switch {
        RatingTreeM rating => _core.MediaItemsM.DataAdapter.All.Values.Where(x => x.Rating == rating.Value).ToList(),
        PersonM person => _core.MediaItemsM.GetMediaItems(person),
        KeywordM keyword => _core.MediaItemsM.GetMediaItems(keyword, recursive),
        GeoNameM geoName => _core.MediaItemsM.GetMediaItems(geoName, recursive),
        _ => new()
      };

      var tabTitle = and || hide
        ? null
        : item switch {
          RatingTreeM rating => rating.Value.ToString(),
          PersonM person => person.Name,
          KeywordM keyword => keyword.Name,
          GeoNameM geoName => geoName.Name,
          _ => string.Empty
        };

      AddThumbnailsGridIfNotActive(tabTitle);
      await Current.LoadMediaItems(items, and, hide);
    }

    public async Task LoadByFolder(ITreeItem item, bool and, bool hide, bool recursive) {
      if (item is FolderM { IsAccessible: false }) return;

      item.IsSelected = true;

      var roots = (item as FolderKeywordM)?.Folders ?? new List<FolderM> { (FolderM)item };
      var folders = FoldersM.GetFolders(roots, recursive)
        .Where(f => _core.FoldersM.IsFolderVisible(f))
        .ToList();

      if (and || hide) {
        var items = folders.SelectMany(x => x.MediaItems).ToList();
        AddThumbnailsGridIfNotActive(null);
        await Current.LoadMediaItems(items, and, hide);
        return;
      }

      AddThumbnailsGridIfNotActive(folders[0].Name);
      await Current.LoadAsync(null, folders);
    }

    public async Task ReloadGridsIfContains(MediaItemM[] mediaItems) {
      foreach (var tg in All) {
        if (!tg.FilteredItems.Any(mediaItems.Contains)) continue;
        if (tg.Equals(Current))
          await tg.ThumbsGridReloadItems();
        else
          tg.NeedReload = true;
      }
    }
  }
}
