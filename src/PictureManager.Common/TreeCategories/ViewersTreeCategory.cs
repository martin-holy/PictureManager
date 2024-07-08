using MH.UI.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.TreeCategories;

public sealed class ViewersTreeCategory(ViewerR r)
  : TreeCategory<ViewerM>(Res.IconEye, "Viewers", (int)Category.Viewers, r) {

  public override void OnItemSelected(object o) =>
    Core.VM.Viewer.OpenDetail(o as ViewerM);
}