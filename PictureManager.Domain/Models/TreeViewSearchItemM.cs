using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchItemM {
    public string IconName { get; }
    public string Title { get; }
    public string ToolTip { get; }
    public ITreeItem Item { get; }
    public TreeCategoryBase Category { get; }

    public TreeViewSearchItemM(string iconName, string title, string toolTip, ITreeItem item, TreeCategoryBase category) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
      Category = category;
    }
  }
}
