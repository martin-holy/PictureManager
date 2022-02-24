using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private readonly WorkTask _workTask = new();
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };
    private ThumbnailsGridVM _current;

    public ThumbnailsGridsM Model { get; }
    public ThumbnailsGridVM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public RelayCommand<string> AddThumbnailsGridCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterAndCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterOrCommand { get; }
    public RelayCommand<IFilterItem> ActivateFilterNotCommand { get; }
    public RelayCommand<ICatTreeViewItem> LoadByTagCommand { get; }
    public RelayCommand<object> ClearFiltersCommand { get; }
    public RelayCommand<object> SelectNotModifiedCommand { get; }
    public RelayCommand<object> ShuffleCommand { get; }
    public RelayCommand<object> CompressCommand { get; }
    public RelayCommand<object> ResizeImagesCommand { get; }
    public RelayCommand<object> ImagesToVideoCommand { get; }
    public RelayCommand<object> CopyPathsCommand { get; }
    public RelayCommand<object> ReapplyFilterCommand { get; }

    public ThumbnailsGridsVM(Core core, AppCore coreVM, ThumbnailsGridsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      #region Commands
      AddThumbnailsGridCommand = new(AddThumbnailsGrid);

      ActivateFilterAndCommand = new(item => _ = ActivateFilter(item, DisplayFilter.And), item => item != null);
      ActivateFilterOrCommand = new(item => _ = ActivateFilter(item, DisplayFilter.Or), item => item != null);
      ActivateFilterNotCommand = new(item => _ = ActivateFilter(item, DisplayFilter.Not), item => item != null);
      ClearFiltersCommand = new(() => _ = ClearFilters());

      LoadByTagCommand = new(
        async item => {
          var (and, hide, recursive) = InputUtils.GetControlAltShiftModifiers();
          await LoadByTag(item, and, hide, recursive);
        },
        item => item != null);
      
      SelectNotModifiedCommand = new(
        () => Model.Current?.SelectNotModified(_core.MediaItemsM.ModifiedItems),
        () => Model.Current?.FilteredItems.Count > 0);
      
      ShuffleCommand = new(
        () => {
          Model.Current.Shuffle();
          _ = ThumbsGridReloadItems();
        },
        () => Model.Current?.FilteredItems.Count > 0);

      CompressCommand = new(
        () => CompressDialog.Open(),
        () => Model.Current?.FilteredItems.Count > 0);

      ResizeImagesCommand = new(
        () => ResizeImagesDialog.Show(Model.Current.GetSelectedOrAll()),
        () => Model.Current?.FilteredItems.Count > 0);

      ImagesToVideoCommand = new(
        ImagesToVideo,
        () => Model.Current?.FilteredItems.Count(x => x.IsSelected && x.MediaType == MediaType.Image) > 1);

      CopyPathsCommand = new(
        () => Clipboard.SetText(string.Join("\n", Model.Current.FilteredItems.Where(x => x.IsSelected).Select(x => x.FilePath))),
        () => Model.Current?.FilteredItems.Count(x => x.IsSelected) > 0);

      ReapplyFilterCommand = new(async () => await ReapplyFilter());
      #endregion
    }

    public void CloseGrid(ThumbnailsGridVM grid) {
      grid.Panel.ClearRows();
      grid.Model.ClearItBeforeLoad();
      Model.All.Remove(grid.Model);
      Current = null;
      Model.Current = null;
    }

    public async Task SetCurrentGrid(ThumbnailsGridVM grid) {
      Current = grid;
      Model.Current = ThumbnailsGridM.ActivateThumbnailsGrid(Model.Current, Current?.Model);
      Current?.Model.UpdateSelected();
      
      if (Current?.Model.NeedReload == true) {
        await ThumbsGridReloadItems();
        Current?.Model.UpdatePositionSlashCount();
      }
      else {
        //TODO this causes error
        //ScrollToCurrentMediaItem();
      }
    }

    public void ScrollToCurrentMediaItem() {
      if (_core.MediaItemsM.Current == null)
        ScrollToTop();
      else
        ScrollTo(_core.MediaItemsM.Current);
    }

    public void ScrollToTop() {
      Current?.Panel.ScrollToTop();
      // TODO
      App.MainWindowV.UpdateLayout();
    }

    public void ScrollTo(MediaItemM mi) => Current?.Panel.ScrollTo(mi);

    private void AddThumbnailsGridIfNotActive(string tabTitle) {
      if (_coreVM.MainTabsVM.Selected?.Content is ThumbnailsGridVM thumbsGridVM) {
        if (tabTitle != null)
          _coreVM.MainTabsVM.Selected.ContentHeader = tabTitle;

        return;
      }

      AddThumbnailsGrid(tabTitle);
    }

    private void AddThumbnailsGrid(string tabTitle) {
      var model = Model.AddThumbnailsGrid();
      var viewModel = new ThumbnailsGridVM(_core, _coreVM, model, tabTitle);

      // TODO check all usage and use it less
      model.SelectionChangedEventHandler += (_, _) => {
        _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
        _coreVM.StatusPanelVM.OnPropertyChanged(nameof(_coreVM.StatusPanelVM.FileSize));
      };

      _coreVM.MainTabsVM.AddItem(viewModel.MainTabsItem);
    }

    public async Task LoadByTag(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      // TODO move getting items and tabTitle to model
      var items = item switch {
        RatingTreeVM rating => _core.MediaItemsM.All.Where(x => x.Rating == rating.Value).ToList(),
        PersonTreeVM person => _core.MediaItemsM.GetMediaItems(person.Model),
        KeywordTreeVM keyword => _core.MediaItemsM.GetMediaItems(keyword.Model, recursive),
        GeoNameTreeVM geoName => _core.MediaItemsM.GetMediaItems(geoName.Model, recursive),
        _ => new()
      };

      var tabTitle = and || hide
        ? null
        : item switch {
          RatingTreeVM rating => rating.Value.ToString(),
          PersonTreeVM person => person.Model.Name,
          KeywordTreeVM keyword => keyword.Model.Name,
          GeoNameTreeVM geoName => geoName.Model.Name,
          _ => string.Empty
        };

      AddThumbnailsGridIfNotActive(tabTitle);
      await LoadMediaItems(items, and, hide);
    }

    public async Task LoadByFolder(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      if (item is FolderTreeVM folder && !folder.Model.IsAccessible) return;

      item.IsSelected = true;

      var roots = (item as FolderKeywordTreeVM)?.Model.Folders ?? new List<FolderM> { ((FolderTreeVM)item).Model };
      var folders = FoldersM.GetFolders(roots, recursive)
        .Where(f => _core.FoldersM.IsFolderVisible(f))
        .ToList();

      if (and || hide) {
        var items = folders.SelectMany(x => x.MediaItems).ToList();
        AddThumbnailsGridIfNotActive(null);
        await LoadMediaItems(items, and, hide);
        return;
      }

      AddThumbnailsGridIfNotActive(folders[0].Name);
      await LoadAsync(null, folders);
      // TODO move this up, check for changes before update
      App.Ui.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
    }

    private async Task LoadMediaItems(List<MediaItemM> items, bool and, bool hide) {
      // if CTRL is pressed, add new items to already loaded items
      if (and)
        items = Current.Model.LoadedItems.Union(items).ToList();

      // if ALT is pressed, remove new items from already loaded items
      if (hide)
        items = Current.Model.LoadedItems.Except(items).ToList();

      await LoadAsync(items, null);
      // TODO move this up, check for changes before update
      App.Ui.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
    }

    private async Task LoadAsync(List<MediaItemM> mediaItems, List<FolderM> folders) {
      await _workTask.Cancel();
      ScrollToTop();

      // Clear before new load
      Current.Model.ClearItBeforeLoad();
      // TODO move this elsewhere
      App.MainWindowV.ImageComparerTool.Close();
      // TODO set this to false when finished
      _core.TitleProgressBarM.IsIndeterminate = true;

      await _workTask.Start(Task.Run(async () => {
        var items = await _core.MediaItemsM.GetMediaItemsForLoadAsync(mediaItems, folders, _workTask.Token);
        Current.Model.LoadedItems.AddRange(items);
        await Current.Model.ReloadFilteredItems();
        await LoadThumbnailsAsync(Current.Model.FilteredItems.ToArray(), _workTask.Token);
        Current.Model.SetMediaItemFilterSizeRange();
      }));
    }

    public async Task ThumbsGridReloadItems() {
      if (!await _workTask.Cancel()) return;
      if (Current == null) return;

      ScrollToTop();
      Current.Panel.ClearRows();

      await _workTask.Start(Task.Run(async () =>
        await LoadThumbnailsAsync(Current.Model.FilteredItems.ToArray(), _workTask.Token)));

      Current.Model.NeedReload = false;
      ScrollToCurrentMediaItem();
    }

    private async Task LoadThumbnailsAsync(IReadOnlyCollection<MediaItemM> items, CancellationToken token) {
      _core.TitleProgressBarM.IsIndeterminate = false;
      _core.TitleProgressBarM.ResetProgressBars(100);

      await Task.Run(async () => {
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);
        // create thumbnails
        var progress = new Progress<int>(x => _core.TitleProgressBarM.ValueB = x);
        var thumbs = Imaging.CreateThumbnailsAsync(items, Settings.Default.ThumbnailSize, Settings.Default.JpegQualityLevel, progress, token);
        
        await Task.WhenAll(metadata, thumbs);

        if (token.IsCancellationRequested)
          await Core.RunOnUiThread(() => _coreVM.MediaItemsVM.Delete(_core.MediaItemsM.All.Where(x => x.IsNew).ToArray()));
      }, token);

      // TODO: is this necessary?
      if (Current?.Model.CurrentMediaItem != null) {
        Current.Model.SetSelected(Current.Model.CurrentMediaItem, false);
        Current.Model.SetSelected(Current.Model.CurrentMediaItem, true);
      }

      _core.TitleProgressBarM.ValueA = 100;
      _core.TitleProgressBarM.ValueB = 100;

      GC.Collect();
    }

    private async Task ReadMetadataAndListThumbsAsync(IReadOnlyCollection<MediaItemM> items, CancellationToken token) {
      await Core.RunOnUiThread(() => Current.Panel.ClearRows());

      await Task.Run(async () => {
        var count = items.Count;
        var workingOn = 0;

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;

          workingOn++;
          var percent = Convert.ToInt32((double)workingOn / count * 100);

          if (mi.IsNew) {
            var success = await _coreVM.MediaItemsVM.ReadMetadata(mi);
            mi.IsNew = false;
            if (!success) {
              // delete corrupted MediaItems
              await Core.RunOnUiThread(() => {
                Current.Model.LoadedItems.Remove(mi);
                Current.Model.FilteredItems.Remove(mi);
                Current.Model.UpdatePositionSlashCount();
                _core.MediaItemsM.Delete(mi);
                _core.TitleProgressBarM.ValueA = percent;
              });

              continue;
            }
          }

          await AddMediaItemToGrid(mi);

          await Core.RunOnUiThread(() => {
            mi.SetInfoBox();
            _core.TitleProgressBarM.ValueA = percent;
          });
        }
      }, token);
    }

    private async Task AddMediaItemToGrid(MediaItemM mi) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      var groupItems = new List<VirtualizingWrapPanelGroupItem>();

      if (Current.Model.GroupByFolders) {
        var folderName = mi.Folder.Name;
        var iOfL = folderName.FirstIndexOfLetter();
        var title = iOfL == 0 || folderName.Length - 1 == iOfL ? folderName : folderName[iOfL..];
        var toolTip = mi.Folder.FolderKeyword != null
          ? mi.Folder.FolderKeyword.FullPath
          : mi.Folder.FullPath;
        groupItems.Add(new() { Icon = "IconFolder", Title = title, ToolTip = toolTip });
      }

      if (Current.Model.GroupByDate) {
        var title = DateTimeExtensions.DateTimeFromString(mi.FileName, _dateFormats, null);
        if (!string.IsNullOrEmpty(title))
          groupItems.Add(new() { Icon = "IconCalendar", Title = title });
      }

      await Core.RunOnUiThread(() => {
        Current.Panel.AddGroupIfNew(groupItems.ToArray());
        Current.Panel.AddItem(mi, mi.ThumbWidth + itemOffset);
      });
    }

    public async Task ActivateFilter(IFilterItem item, DisplayFilter displayFilter) {
      Current?.Model.SetDisplayFilter(item, displayFilter);
      await ReapplyFilter();
    }

    public async Task ReapplyFilter() {
      if (Current != null)
        await Current.Model.ReloadFilteredItems();

      _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
      await ThumbsGridReloadItems();
    }

    private async Task ClearFilters() {
      Current?.Model.ClearFilters();
      await ReapplyFilter();
    }

    private void ImagesToVideo() {
      ImagesToVideoDialog.Show(Current.Model.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image),
        async (folder, fileName) => {
          // create new MediaItem, Read Metadata and Create Thumbnail
          var mi = new MediaItemM(_core.MediaItemsM.DataAdapter.GetNextId(), folder, fileName);
          _core.MediaItemsM.All.Add(mi);
          _core.MediaItemsM.OnPropertyChanged(nameof(_core.MediaItemsM.MediaItemsCount));
          folder.MediaItems.Add(mi);
          await _coreVM.MediaItemsVM.ReadMetadata(mi);
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, 0, Settings.Default.JpegQualityLevel);

          // reload grid
          Current.Model.LoadedItems.AddInOrder(mi,
            (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase) >= 0);
          await ReapplyFilter();
          ScrollTo(mi);
        }
      );
    }
  }
}
