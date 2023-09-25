using MH.UI.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class ViewersTreeCategory : TreeCategory<ViewerM> {
  public ViewersTreeCategory() : base(Res.IconEye, "Viewers", (int)Category.Viewers) {
    DataAdapter = Core.Db.Viewers;
  }

  public override void OnItemSelected(object o) =>
    Core.ViewersM.OpenDetail(o as ViewerM);
}