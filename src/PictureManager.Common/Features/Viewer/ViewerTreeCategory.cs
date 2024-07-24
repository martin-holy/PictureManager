using MH.UI.BaseClasses;

namespace PictureManager.Common.Features.Viewer;

public sealed class ViewerTreeCategory(ViewerR r)
  : TreeCategory<ViewerM>(Res.IconEye, "Viewers", (int)Category.Viewers, r) {

  public override void OnItemSelected(object o) =>
    Core.VM.Viewer.OpenDetail(o as ViewerM);
}