using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchItemM {
    public string Icon { get; }
    public string Title { get; }
    public string ToolTip { get; }
    public ITreeItem Item { get; }
    public TreeCategoryBase Category { get; }

    public TreeViewSearchItemM(string icon, string title, string toolTip, ITreeItem item, TreeCategoryBase category) {
      Icon = icon;
      Title = title;
      ToolTip = toolTip;
      Item = item;
      Category = category;
    }
  }
}
