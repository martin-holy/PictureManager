using Android.Views;
using MH.UI.Android.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public class RatingTreeItemViewHolderFactory : ITreeItemViewHolderFactory {
  public int GetViewType(FlatTreeItem item) =>
    item.TreeItem switch {
      RatingTreeM => 1,
      _ => 0
    };

  public FlatTreeItemViewHolderBase Create(ViewGroup parent, int viewType, IAndroidTreeViewHost host) =>
    viewType switch {
      1 => new RatingTreeV(parent.Context!, (TreeViewHost)host),
      _ => new FlatTreeItemViewHolder(parent.Context!, (TreeViewHost)host)
    };
}