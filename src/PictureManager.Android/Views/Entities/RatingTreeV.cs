using Android.Content;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.TreeViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public sealed class RatingTreeV : FlatTreeItemV {
  private readonly RatingV _ratingV;
  protected override bool _showName => false;

  public RatingTreeV(Context context, TreeViewHost treeViewHost) : base(context, treeViewHost) {
    _ratingV = new RatingV(context);
    AddView(_ratingV, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing, 0,0,0));
  }

  public override void Bind(FlatTreeItem? item) {
    base.Bind(item);
    if (item?.TreeItem is RatingTreeM rt)
      _ratingV.Bind(rt.Rating);
  }
}