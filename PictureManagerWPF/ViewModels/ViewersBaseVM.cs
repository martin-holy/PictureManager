using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class ViewersBaseVM {
    private readonly AppCore _coreVM;

    public ViewersM Model { get; }

    public ViewersBaseVM(AppCore coreVM, ViewersM model) {
      _coreVM = coreVM;
      Model = model;
    }

    public void UpdateCategoryGroupsVisibility(ViewerM viewer) {
      foreach (var g in _coreVM.CategoryGroupsTreeVM.All.Values)
        g.IsHidden = viewer.ExcCatGroupsIds.Contains(g.BaseVM.Model.Id);
    }
  }
}
