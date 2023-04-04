using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System.Threading.Tasks;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private ThumbnailsGridVM _current;

    public ThumbnailsGridsM Model { get; }
    public ThumbnailsGridVM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public ThumbnailsGridsVM(Core core, AppCore coreVM, ThumbnailsGridsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.ThumbnailsGridAddedEventHandler += (_, e) => {
        var viewModel = new ThumbnailsGridVM(_core, _coreVM, e.Data.Item1, e.Data.Item2);
        _core.MainTabsM.AddItem(viewModel.MainTabsItem);
      };

      Model.AddThumbnailsGridIfNotActive = AddThumbnailsGridIfNotActive;
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
  }
}
