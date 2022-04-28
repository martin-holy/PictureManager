using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class TreeViewSearchItemM {
    public string IconName { get; }
    public string Title { get; }
    public string ToolTip { get; }
    public ITreeItem Item { get; }

    public TreeViewSearchItemM(string iconName, string title, string toolTip, ITreeItem item) {
      IconName = iconName;
      Title = title;
      ToolTip = toolTip;
      Item = item;
    }
  }
}
