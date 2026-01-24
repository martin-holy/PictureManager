using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public sealed class RatingTreeV : FlatTreeItemViewHolder {
  private readonly RatingV _ratingV;
  protected override bool _showName => false;

  public RatingTreeV(Context context, TreeViewHost treeViewHost) : base(context, treeViewHost) {
    _ratingV = new RatingV(context);
    _container.AddView(_ratingV, new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing, 0,0,0));
  }

  public override void Bind(FlatTreeItem? item) {
    base.Bind(item);
    if (item?.TreeItem is RatingTreeM rt)
      _ratingV.Bind(rt.Rating);
  }
}