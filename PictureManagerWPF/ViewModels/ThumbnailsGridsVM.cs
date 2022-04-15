using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
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

      ActivateFilterAndCommand = new(item => _ = Model.Current?.ActivateFilter(item, DisplayFilter.And), item => item != null);
      ActivateFilterOrCommand = new(item => _ = Model.Current?.ActivateFilter(item, DisplayFilter.Or), item => item != null);
      ActivateFilterNotCommand = new(item => _ = Model.Current?.ActivateFilter(item, DisplayFilter.Not), item => item != null);
      ClearFiltersCommand = new(() => _ = Model.Current?.ClearFilters());

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
          _ = Model.Current.ThumbsGridReloadItems();
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

      ReapplyFilterCommand = new(
        async () => await Model.Current.ReapplyFilter(),
        () => Model.Current != null);
      #endregion
    }

    public void CloseGrid(ThumbnailsGridVM grid) {
      grid.Model.ClearItBeforeLoad();
      Model.All.Remove(grid.Model);

      if (grid.Equals(Current)) {
        Current = null;
        Model.Current = null;
      }
    }

    public async Task SetCurrentGrid(ThumbnailsGridVM grid) {
      Current = grid;
      await Model.SetCurrentGrid(grid?.Model);
    }

    private void AddThumbnailsGridIfNotActive(string tabTitle) {
      if (_coreVM.MainTabsVM.Selected?.Content is ThumbnailsGridVM) {
        if (tabTitle != null)
          _coreVM.MainTabsVM.Selected.ContentHeader = tabTitle;

        return;
      }

      AddThumbnailsGrid(tabTitle);
    }

    private void AddThumbnailsGrid(string tabTitle) {
      var model = Model.AddThumbnailsGrid(_core.MediaItemsM, _core.TitleProgressBarM);
      var viewModel = new ThumbnailsGridVM(_coreVM, model, tabTitle);

      model.SelectionChangedEventHandler += (_, _) => {
        _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
        _coreVM.StatusPanelVM.OnPropertyChanged(nameof(_coreVM.StatusPanelVM.FileSize));
      };

      model.FilteredChangedEventHandler += (_, _) => {
        _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
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
      await Model.Current.LoadMediaItems(items, and, hide);
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
        await Model.Current.LoadMediaItems(items, and, hide);
        return;
      }

      AddThumbnailsGridIfNotActive(folders[0].Name);
      await Model.Current.LoadAsync(null, folders);
    }

    private void ImagesToVideo() {
      ImagesToVideoDialog.Show(Model.Current.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image),
        async (folder, fileName) => {
          // create new MediaItem, Read Metadata and Create Thumbnail
          var mi = new MediaItemM(_core.MediaItemsM.DataAdapter.GetNextId(), folder, fileName);
          _core.MediaItemsM.All.Add(mi);
          _core.MediaItemsM.OnPropertyChanged(nameof(_core.MediaItemsM.MediaItemsCount));
          folder.MediaItems.Add(mi);
          await _core.MediaItemsM.ReadMetadata(mi, false);
          mi.SetThumbSize(true);

          // reload grid
          Model.Current.LoadedItems.AddInOrder(mi,
            (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase) >= 0);
          await Model.Current.ReapplyFilter();
          Model.Current.ScrollToItem = mi;
        }
      );
    }
  }
}
