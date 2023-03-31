using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MH.Utils.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.Domain.Dialogs;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private ThumbnailsGridVM _current;

    public ThumbnailsGridsM Model { get; }
    public ThumbnailsGridVM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public RelayCommand<object> LoadByTagCommand { get; }
    public RelayCommand<object> CompressCommand { get; }
    public RelayCommand<object> ResizeImagesCommand { get; }
    public RelayCommand<object> ImagesToVideoCommand { get; }
    public RelayCommand<object> CopyPathsCommand { get; }

    public ThumbnailsGridsVM(Core core, AppCore coreVM, ThumbnailsGridsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.ThumbnailsGridAddedEventHandler += (_, e) => {
        var viewModel = new ThumbnailsGridVM(_core, _coreVM, e.Data.Item1, e.Data.Item2);
        _core.MainTabsM.AddItem(viewModel.MainTabsItem);
      };

      Model.AddThumbnailsGridIfNotActive = AddThumbnailsGridIfNotActive;

      #region Commands
      LoadByTagCommand = new(
        async item => {
          var (and, hide, recursive) = InputUtils.GetControlAltShiftModifiers();
          await Model.LoadByTag(item, and, hide, recursive);
        },
        item => item != null);

      CompressCommand = new(
        () => {
          Core.DialogHostShow(
            new CompressDialogM(
              Model.Current.GetSelectedOrAll()
                .Where(x => x.MediaType == MediaType.Image).ToList(),
              Settings.Default.JpegQualityLevel));
        },
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
      if (_core.MainTabsM.Selected?.Content is ThumbnailsGridVM) {
        if (tabTitle != null)
          _core.MainTabsM.Selected.ContentHeader = tabTitle;

        return;
      }

      Model.AddThumbnailsGrid(tabTitle);
    }

    private void ImagesToVideo() {
      ImagesToVideoDialog.Show(Model.Current.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image),
        async (folder, fileName) => {
          // create new MediaItem, Read Metadata and Create Thumbnail
          var mi = _core.MediaItemsM.AddNew(folder, fileName, false, true);

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
