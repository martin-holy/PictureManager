using MH.UI.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.TreeCategories;

public sealed class ViewersTreeCategory : TreeCategory<ViewerM> {
  public ViewersTreeCategory(ViewerR r) :
    base(Res.IconEye, "Viewers", (int)Category.Viewers) {
    DataAdapter = r;
  }

  public override void OnItemSelected(object o) =>
    Core.VM.Viewer.OpenDetail(o as ViewerM);
}