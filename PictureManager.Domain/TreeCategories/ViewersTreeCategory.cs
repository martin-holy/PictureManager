using MH.UI.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.TreeCategories;

public sealed class ViewersTreeCategory : TreeCategory<ViewerM> {
  public ViewersTreeCategory(ViewerR r) :
    base(Res.IconEye, "Viewers", (int)Category.Viewers) {
    DataAdapter = r;
  }

  public override void OnItemSelected(object o) =>
    Core.S.Viewer.OpenDetail(o as ViewerM);
}