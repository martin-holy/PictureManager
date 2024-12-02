using MH.UI.BaseClasses;

namespace PictureManager.Common.Features.Viewer;

public sealed class ViewerTreeCategory(ViewerR r)
  : TreeCategory<ViewerM>(new(), Res.IconEye, "Viewers", (int)Category.Viewers, r) {

  protected override void _onItemSelected(object o) =>
    Core.VM.Viewer.OpenDetail(o as ViewerM);
}