using MH.UI.WPF.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class ViewersVM {
    private readonly AppCore _coreVM;

    public ViewersM Model { get; }
    public RelayCommand<ViewerM> SetCurrentCommand { get; }

    public ViewersVM(AppCore coreVM, ViewersM model) {
      _coreVM = coreVM;
      Model = model;

      SetCurrentCommand = new(SetCurrent);
    }

    public void SetCurrent(ViewerM viewer) {
      Model.SetCurrent(viewer);
      _coreVM.FoldersTreeVM.UpdateDrivesVisibility();
    }
  }
}
