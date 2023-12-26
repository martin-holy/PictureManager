using MH.UI.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class ViewersTreeCategory : TreeCategory<ViewerM> {
  public ViewersTreeCategory(ViewersDA da) :
    base(Res.IconEye, "Viewers", (int)Category.Viewers) {
    DataAdapter = da;
  }

  public override void OnItemSelected(object o) =>
    Core.ViewersM.OpenDetail(o as ViewerM);
}