using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class ViewersBaseVM {
    private readonly Core _core;

    public Viewers Model { get; }

    public ViewersBaseVM(Core core, Viewers model) {
      _core = core;
      Model = model;
    }

    public void UpdateCategoryGroupsVisibility(Viewer viewer) {
      foreach (var g in App.Ui.CategoryGroupsTreeVM.All.Values)
        g.IsHidden = viewer.ExcCatGroupsIds.Contains(g.BaseVM.Model.Id);
    }
  }
}
