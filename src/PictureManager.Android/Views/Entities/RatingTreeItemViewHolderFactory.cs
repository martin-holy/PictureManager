using Android.Views;
using AndroidX.RecyclerView.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.TreeViewHost;
using MH.UI.Android.Controls.Recycler;
using MH.UI.Android.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public class RatingTreeItemViewHolderFactory : ITreeItemViewHolderFactory {
  public int GetViewType(FlatTreeItem item) =>
    item.TreeItem switch {
      RatingTreeM => 1,
      _ => 0
    };

  public RecyclerView.ViewHolder Create(ViewGroup parent, int viewType, IAndroidTreeViewHost host) =>
    viewType switch {
      1 => new BaseViewHolder(new RatingTreeV(parent.Context!, (TreeViewHost)host), new(LPU.Match, LPU.Wrap)),
      _ => new BaseViewHolder(new FlatTreeItemV(parent.Context!, (TreeViewHost)host), new(LPU.Match, LPU.Wrap))
    };
}